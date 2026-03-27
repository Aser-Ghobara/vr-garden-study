using Bhaptics.SDK2;
using UnityEngine;

/// <summary>
/// Simple button-friendly wrapper around the bHaptics SDK.
/// Attach this to a GameObject, set a default event name, and call the public methods from Unity UI.
/// </summary>
public class HapticsController : MonoBehaviour
{
    [Header("Default Event")]
    [Tooltip("The registered bHaptics event key to play when using PlayDefaultHaptic.")]
    [SerializeField] private string defaultEventName = "";

    [Header("Playback")]
    [SerializeField] [Range(0f, 1f)] private float intensity = 1f;
    [SerializeField] [Range(0f, 1f)] private float duration = 1f;
    [SerializeField] [Range(-180f, 180f)] private float angleX = 0f;
    [SerializeField] [Range(0f, 1f)] private float offsetY = 0.5f;

    private int lastRequestId = -1;

    /// <summary>
    /// Plays the default event configured in the Inspector.
    /// Good for Unity button OnClick bindings.
    /// </summary>
    public void PlayDefaultHaptic()
    {
        if (string.IsNullOrWhiteSpace(defaultEventName))
        {
            Debug.LogWarning("[HapticsController] Default event name is empty.");
            return;
        }

        PlayHaptic(defaultEventName);
    }

    /// <summary>
    /// Plays the given bHaptics event with the configured intensity, duration, angle, and offset.
    /// </summary>
    public void PlayHaptic(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            Debug.LogWarning("[HapticsController] Event name is empty.");
            return;
        }

        lastRequestId = BhapticsLibrary.PlayParam(eventName, intensity, duration, angleX, offsetY);
        Debug.Log($"[HapticsController] Playing haptic event '{eventName}' with request id {lastRequestId}.");
    }

    /// <summary>
    /// Plays the default event in a repeating loop until stopped.
    /// </summary>
    public void LoopDefaultHaptic()
    {
        if (string.IsNullOrWhiteSpace(defaultEventName))
        {
            Debug.LogWarning("[HapticsController] Default event name is empty.");
            return;
        }

        LoopHaptic(defaultEventName);
    }

    /// <summary>
    /// Plays the given bHaptics event in a repeating loop until stopped.
    /// </summary>
    public void LoopHaptic(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            Debug.LogWarning("[HapticsController] Event name is empty.");
            return;
        }

        lastRequestId = BhapticsLibrary.PlayLoop(eventName, intensity, duration, angleX, offsetY);
        Debug.Log($"[HapticsController] Looping haptic event '{eventName}' with request id {lastRequestId}.");
    }

    /// <summary>
    /// Stops the last request started by this controller, if any.
    /// </summary>
    public void StopLastHaptic()
    {
        if (lastRequestId < 0)
        {
            Debug.Log("[HapticsController] No active request to stop.");
            return;
        }

        BhapticsLibrary.StopInt(lastRequestId);
        Debug.Log($"[HapticsController] Stopped request id {lastRequestId}.");
        lastRequestId = -1;
    }

    /// <summary>
    /// Stops all currently playing bHaptics events.
    /// Good for Unity button OnClick bindings.
    /// </summary>
    public void StopAllHaptics()
    {
        BhapticsLibrary.StopAll();
        Debug.Log("[HapticsController] Stopped all haptics.");
        lastRequestId = -1;
    }
}
