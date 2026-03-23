using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Bridges Unity to a local external haptics app over TCP.
///
/// Setup:
/// 1. Add this component to any GameObject in your scene.
/// 2. Set "External Exe Path" to HapticsAudioPlayer.exe in the Inspector.
/// 3. Optionally set "Default Audio File Path" for quick testing.
/// 4. Call PlayHaptics, LoopHaptics, or StopHaptics from your other scripts.
///
/// The controller will:
/// - Launch the external app if needed
/// - Connect to localhost:5050
/// - Send commands without blocking the Unity main thread
/// - Reconnect automatically if the app is restarted
/// </summary>
public class ExternalHapticsController : MonoBehaviour
{
    [Header("External App")]
    [Tooltip("Full path to HapticsAudioPlayer.exe.")]
    public string externalExePath;

    [Tooltip("Optional default haptics audio file used by the example methods.")]
    public string defaultAudioFilePath;

    [Header("Connection")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5050;
    [SerializeField] private float reconnectDelaySeconds = 1.5f;
    [SerializeField] private float commandPollDelaySeconds = 0.05f;

    private readonly ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> mainThreadLogs = new ConcurrentQueue<string>();
    private readonly object connectionLock = new object();

    private CancellationTokenSource cancellationTokenSource;
    private Task workerTask;
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private bool hasLoggedConnected;
    private bool isQuitting;

    private void Start()
    {
        StartWorkerIfNeeded();
    }

    private void Update()
    {
        while (mainThreadLogs.TryDequeue(out string message))
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// Sends a one-shot play command to the external app.
    /// </summary>
    public void PlayHaptics(string filePath)
    {
        EnqueueCommand("play", filePath);
    }

    /// <summary>
    /// Sends a looping play command to the external app.
    /// </summary>
    public void LoopHaptics(string filePath)
    {
        EnqueueCommand("loop", filePath);
    }

    /// <summary>
    /// Sends a stop command to the external app.
    /// </summary>
    public void StopHaptics()
    {
        commandQueue.Enqueue("stop");
        LogMainThread("[ExternalHapticsController] Queued command: stop");
        StartWorkerIfNeeded();
    }

    /// <summary>
    /// Example helper for quick Inspector button wiring.
    /// Uses the Default Audio File Path field.
    /// </summary>
    public void StartHapticsExample()
    {
        if (string.IsNullOrWhiteSpace(defaultAudioFilePath))
        {
            Debug.LogWarning("[ExternalHapticsController] Default audio file path is empty.");
            return;
        }

        PlayHaptics(defaultAudioFilePath);
    }

    /// <summary>
    /// Example helper for quick Inspector button wiring.
    /// </summary>
    public void StopHapticsExample()
    {
        StopHaptics();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        Shutdown();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void EnqueueCommand(string command, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogWarning($"[ExternalHapticsController] Cannot send '{command}' because filePath is empty.");
            return;
        }

        string normalizedPath = filePath.Trim();
        string payload = $"{command}|{normalizedPath}";
        commandQueue.Enqueue(payload);
        LogMainThread($"[ExternalHapticsController] Queued command: {payload}");
        StartWorkerIfNeeded();
    }

    private void StartWorkerIfNeeded()
    {
        if (isQuitting)
        {
            return;
        }

        if (workerTask != null && !workerTask.IsCompleted)
        {
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        workerTask = Task.Run(() => ConnectionWorker(cancellationTokenSource.Token), cancellationTokenSource.Token);
        LogMainThread("[ExternalHapticsController] Background worker started.");
    }

    private async Task ConnectionWorker(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                EnsureExternalAppRunning();

                if (!IsConnected())
                {
                    await ConnectAsync(token);
                }

                if (!IsConnected())
                {
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
                    continue;
                }

                if (commandQueue.TryDequeue(out string command))
                {
                    if (!await TrySendCommandAsync(command, token))
                    {
                        commandQueue.Enqueue(command);
                        await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(commandPollDelaySeconds), token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMainThread($"[ExternalHapticsController] Worker error: {ex.Message}");
                CloseConnection();
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
            }
        }

        CloseConnection();
        LogMainThread("[ExternalHapticsController] Background worker stopped.");
    }

    private void EnsureExternalAppRunning()
    {
        string processName = GetProcessName();
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        Process[] matches = Process.GetProcessesByName(processName);
        if (matches != null && matches.Length > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(externalExePath))
        {
            LogMainThread("[ExternalHapticsController] External exe path is empty. Cannot launch HapticsAudioPlayer.");
            return;
        }

        if (!File.Exists(externalExePath))
        {
            LogMainThread($"[ExternalHapticsController] External exe not found: {externalExePath}");
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = externalExePath,
                WorkingDirectory = Path.GetDirectoryName(externalExePath) ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            LogMainThread($"[ExternalHapticsController] Launched external app: {externalExePath}");
        }
        catch (Exception ex)
        {
            LogMainThread($"[ExternalHapticsController] Failed to launch external app: {ex.Message}");
        }
    }

    private async Task ConnectAsync(CancellationToken token)
    {
        CloseConnection();

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port);

            lock (connectionLock)
            {
                tcpClient = client;
                networkStream = client.GetStream();
            }

            if (!hasLoggedConnected)
            {
                LogMainThread($"[ExternalHapticsController] Connected to {host}:{port}");
                hasLoggedConnected = true;
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            hasLoggedConnected = false;
            LogMainThread($"[ExternalHapticsController] Connection failed: {ex.Message}");
        }
    }

    private async Task<bool> TrySendCommandAsync(string command, CancellationToken token)
    {
        NetworkStream stream;

        lock (connectionLock)
        {
            stream = networkStream;
        }

        if (stream == null || !IsConnected())
        {
            hasLoggedConnected = false;
            return false;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(command + "\n");
            await stream.WriteAsync(data, 0, data.Length, token);
            await stream.FlushAsync(token);
            LogMainThread($"[ExternalHapticsController] Sent command: {command}");
            return true;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            hasLoggedConnected = false;
            LogMainThread($"[ExternalHapticsController] Send failed: {ex.Message}");
            CloseConnection();
            return false;
        }
    }

    private bool IsConnected()
    {
        lock (connectionLock)
        {
            return tcpClient != null && tcpClient.Connected && networkStream != null;
        }
    }

    private string GetProcessName()
    {
        if (!string.IsNullOrWhiteSpace(externalExePath))
        {
            return Path.GetFileNameWithoutExtension(externalExePath);
        }

        return "HapticsAudioPlayer";
    }

    private void CloseConnection()
    {
        lock (connectionLock)
        {
            if (networkStream != null)
            {
                networkStream.Close();
                networkStream = null;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
        }
    }

    private void Shutdown()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        CloseConnection();
    }

    private void LogMainThread(string message)
    {
        mainThreadLogs.Enqueue(message);
    }
}
