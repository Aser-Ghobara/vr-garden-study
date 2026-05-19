using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ExperimentFlow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the TrialManager in the Inspector (usually on your ExperimentManager object).")]
    public TrialManager trialManager;

    [Tooltip("Assign the GameObject containing trial video visuals.")]
    public GameObject videoGroup;

    [Tooltip("Assign the GameObject containing reflection UI/content.")]
    public GameObject reflectionGroup;

    [Tooltip("Assign the GameObject containing garden visuals.")]
    public GameObject gardenGroup;

    [Tooltip("Assign the VideoPlayer used to present the happy clip.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Assign the GardenController component on your garden root object.")]
    public GardenController gardenController;
    public HapticsController hapticsController;
    public HapticVestController hapticVestController;
    public ExternalHapticsController externalHapticsController;
    public VideoPhaseController videoPhaseController;

    public TrialTransitionController transitionController;

    [Tooltip("Assign the happy clip to play when StartVideo is pressed.")]
    public VideoClip happyVideoClip;

    [Tooltip("Optional: assign the Start Garden button to enable it after reflection recording.")]
    public Button startGardenButton;

    [Header("bHaptics Events")]
    [Tooltip("Plays during phase 3 for responsive + haptic trials.")]
    public string responsivePhase3HapticEventName;

    [Tooltip("Plays during the recovery phase for responsive + haptic trials.")]
    public string responsiveRecoveryHapticEventName;

    [Tooltip("Plays after reflection for non-responsive + haptic trials.")]
    public string nonResponsiveGardenHapticEventName;

    [Tooltip("How often to re-trigger the non-responsive garden haptic if the bHaptics loop stops early.")]
    public float nonResponsiveGardenHapticRefreshSeconds = 2f;

    [Header("Reflection Recording")]
    [Tooltip("Participant identifier included in saved reflection recording filenames.")]
    public string participantId = "participant";
    [Tooltip("Folder name inside Application.persistentDataPath where reflection recordings are saved.")]
    public string reflectionRecordingFolderName = "ReflectionRecordings";
    [Tooltip("Optional: existing reflection prompt text that will display the countdown.")]
    public TMP_Text reflectionPromptText;

    private Coroutine startVideoRoutine;
    private Coroutine delayedHapticRoutine;
    private Coroutine nonResponsiveHapticLoopRoutine;
    private Coroutine endUIRoutine;
    private string reflectionPromptBaseText;

    private void Start()
    {
        if (gardenGroup != null)
        {
            gardenGroup.SetActive(true);
        }

        if (videoGroup != null)
        {
            videoGroup.SetActive(false);
        }

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        HideEndUI();
    }

    public void StartVideo()
    {
        HideEndUI();

        if (startVideoRoutine != null)
        {
            StopCoroutine(startVideoRoutine);
        }

        startVideoRoutine = StartCoroutine(StartVideoRoutine());
    }

    public void StartGarden()
    {
        HideEndUI();

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        if (gardenGroup != null)
        {
            gardenGroup.SetActive(true);
        }

        if (gardenController == null)
        {
            Debug.LogWarning("ExperimentFlow: GardenController is not assigned.");
            return;
        }

        StartEndUIRoutine(ShowEndUIAfterResponsiveSequence());
    }

    private IEnumerator StartVideoRoutine()
    {
        if (videoGroup != null)
        {
            videoGroup.SetActive(true);
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("ExperimentFlow: VideoPlayer is not assigned.");
        }
        else
        {
            VideoClip clipToPlay = happyVideoClip;
            if (clipToPlay == null && trialManager != null)
            {
                clipToPlay = trialManager.happyClip;
            }

            if (clipToPlay == null)
            {
                Debug.LogWarning("ExperimentFlow: No happy video clip is assigned.");
            }
            else
            {
                videoPlayer.clip = clipToPlay;
            }

            bool videoFinished = false;
            void OnLoopPointReached(VideoPlayer source) => videoFinished = true;

            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.Play();

            yield return new WaitUntil(() => videoFinished || !videoPlayer.isPlaying);
            videoPlayer.loopPointReached -= OnLoopPointReached;
        }

        if (videoGroup != null)
        {
            videoGroup.SetActive(false);
        }

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(true);
        }

        yield return StartCoroutine(RecordAndSaveReflectionAudio("manual"));

        if (startGardenButton != null)
        {
            startGardenButton.interactable = true;
        }

        startVideoRoutine = null;
    }

    // Kept for compatibility with TrialManager references; manual button flow is now used.
    public IEnumerator RunTrialSequence(TrialManager.Trial trial)
    {
        if (trial == null)
        {
            Debug.LogWarning("ExperimentFlow: Trial is null.");
            yield break;
        }

        HideEndUI();

        if (startVideoRoutine != null)
        {
            StopCoroutine(startVideoRoutine);
            startVideoRoutine = null;
        }

        if (gardenController != null)
        {
            gardenController.ResetGardenToNeutral();
            gardenController.ConfigurePhase3Haptics(null);
            gardenController.ConfigureRecoveryHaptics(null);
        }

        StopAllHaptics();

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        if (videoPhaseController == null)
        {
            Debug.LogWarning("ExperimentFlow: VideoPhaseController is not assigned.");
            yield break;
        }

        if (trial.videoClip == null)
        {
            Debug.LogWarning($"ExperimentFlow: Trial {trial.trialIndex} does not have a video clip assigned.");
        }

        videoPhaseController.StartVideoPhase(trial.videoClip, false);
        yield return new WaitUntil(() => videoPhaseController == null || videoPhaseController.IsVideoPhaseComplete);

        bool isResponsive = trial.responsiveness == TrialManager.ResponsivenessType.Responsive;
        int reflectionDurationSeconds = 30;

        if (isResponsive)
        {
            if (reflectionGroup != null)
            {
                reflectionGroup.SetActive(false);
            }

            if (gardenGroup != null)
            {
                gardenGroup.SetActive(true);
            }

            if (transitionController != null)
            {
                yield return StartCoroutine(transitionController.DoTransition(
                    gardenController != null ? gardenController.ApplyPhase3InitialStateImmediate : null));
            }
            else if (gardenController != null)
            {
                yield return gardenController.StartPhase3InitialFade();
            }

            if (gardenController != null)
            {
                gardenController.StartPhase3Buildup(reflectionDurationSeconds);
            }

            if (reflectionGroup != null)
            {
                reflectionGroup.SetActive(true);
            }
        }
        else if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(true);
        }

        Debug.Log($"ExperimentFlow: Starting {reflectionDurationSeconds}-second reflection recording.");
        yield return StartCoroutine(RecordAndSaveReflectionAudio($"trial_{trial.trialIndex}", reflectionDurationSeconds));

        Debug.Log("ExperimentFlow: Reflection phase complete.");

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        if (gardenGroup != null)
        {
            gardenGroup.SetActive(true);
        }

        if (!isResponsive)
        {
            RestoreGardenAmbience();
        }

        if (gardenController == null)
        {
            Debug.LogWarning("ExperimentFlow: GardenController is not assigned.");
            yield break;
        }

        if (gardenGroup != null)
        {
            gardenGroup.SetActive(true);
        }

        if (trial.haptic == TrialManager.HapticType.Haptic &&
            trial.responsiveness == TrialManager.ResponsivenessType.Responsive)
        {
            if (gardenController != null)
            {
                gardenController.ConfigurePhase3Haptics(responsivePhase3HapticEventName);
                gardenController.ConfigureRecoveryHaptics(responsiveRecoveryHapticEventName);
            }
        }
        else
        {
            if (gardenController != null)
            {
                gardenController.ConfigurePhase3Haptics(null);
                gardenController.ConfigureRecoveryHaptics(null);
            }
        }

        if (gardenController != null)
        {
            gardenController.ConfigureRecoveryLighting(trial.trialIndex == 0);
        }

        if (!isResponsive)
        {
            if (trial.haptic == TrialManager.HapticType.Haptic)
            {
                StartNonResponsiveHapticAfterReflection();
            }

            Debug.Log("Non-responsive trial: garden stays neutral.");
            StartEndUIRoutine(ShowEndUIAfterDelay(60f));
            yield break;
        }

        Debug.Log("Responsive trial: running garden sequence from phase 3 after reflection.");
        gardenController.StartResponsiveSequenceFromPhase3();
        yield return new WaitUntil(() => gardenController == null || !gardenController.IsSequenceRunning);
        ShowEndUI();
    }

    private void StartNonResponsiveHapticAfterReflection()
    {
        if (delayedHapticRoutine != null)
        {
            StopCoroutine(delayedHapticRoutine);
        }

        if (nonResponsiveHapticLoopRoutine != null)
        {
            StopCoroutine(nonResponsiveHapticLoopRoutine);
            nonResponsiveHapticLoopRoutine = null;
        }

        delayedHapticRoutine = StartCoroutine(PlayNonResponsiveHapticAfterReflection());
    }

    private IEnumerator PlayNonResponsiveHapticAfterReflection()
    {
        if (hapticsController == null)
        {
            Debug.LogWarning("ExperimentFlow: HapticsController is not assigned.");
            delayedHapticRoutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(nonResponsiveGardenHapticEventName))
        {
            Debug.LogWarning("ExperimentFlow: Non-responsive garden haptic event name is empty.");
            delayedHapticRoutine = null;
            yield break;
        }

        nonResponsiveHapticLoopRoutine = StartCoroutine(LoopNonResponsiveGardenHaptic());
        delayedHapticRoutine = null;
    }

    private IEnumerator LoopNonResponsiveGardenHaptic()
    {
        float refreshSeconds = Mathf.Max(0.1f, nonResponsiveGardenHapticRefreshSeconds);
        bool hasStarted = false;

        while (true)
        {
            if (hasStarted)
            {
                hapticsController.StopLastHaptic();
            }

            hapticsController.LoopHaptic(nonResponsiveGardenHapticEventName);
            hasStarted = true;
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private void StopAllHaptics()
    {
        if (delayedHapticRoutine != null)
        {
            StopCoroutine(delayedHapticRoutine);
            delayedHapticRoutine = null;
        }

        if (nonResponsiveHapticLoopRoutine != null)
        {
            StopCoroutine(nonResponsiveHapticLoopRoutine);
            nonResponsiveHapticLoopRoutine = null;
        }

        if (hapticsController != null)
        {
            hapticsController.StopAllHaptics();
        }

        if (externalHapticsController != null)
        {
            externalHapticsController.StopHaptics();
        }
    }

    public void ForceStopHaptics()
    {
        StopAllHaptics();

        if (gardenController != null)
        {
            gardenController.ConfigurePhase3Haptics(null);
            gardenController.ConfigureRecoveryHaptics(null);
        }
    }

    private void RestoreGardenAmbience()
    {
        if (gardenController == null ||
            gardenController.ambienceSource == null ||
            gardenController.jungleClip == null)
        {
            return;
        }

        gardenController.ambienceSource.Stop();
        gardenController.ambienceSource.clip = gardenController.jungleClip;
        gardenController.ambienceSource.loop = true;
        gardenController.ambienceSource.volume = 0.05f;
        gardenController.ambienceSource.Play();
    }

    private IEnumerator RecordAndSaveReflectionAudio(string recordingLabel, int durationSeconds = 20)
    {
        int recordingLengthSeconds = durationSeconds;
        const int sampleRate = 44100;
        const string microphoneDeviceName = null;

        CacheReflectionPromptBaseText();

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("ExperimentFlow: No microphone device found. Reflection recording skipped.");
            yield return StartCoroutine(UpdateReflectionCountdown(recordingLengthSeconds));
            ResetReflectionPromptText();
            yield break;
        }

        AudioClip recordedClip = Microphone.Start(microphoneDeviceName, false, recordingLengthSeconds, sampleRate);
        if (recordedClip == null)
        {
            Debug.LogWarning("ExperimentFlow: Microphone.Start returned null. Reflection recording skipped.");
            yield return StartCoroutine(UpdateReflectionCountdown(recordingLengthSeconds));
            ResetReflectionPromptText();
            yield break;
        }

        yield return StartCoroutine(UpdateReflectionCountdown(recordingLengthSeconds));

        int recordedSamples = Microphone.GetPosition(microphoneDeviceName);
        if (recordedSamples <= 0)
        {
            recordedSamples = recordedClip.samples;
        }

        if (Microphone.IsRecording(microphoneDeviceName))
        {
            Microphone.End(microphoneDeviceName);
        }

        ResetReflectionPromptText();

        if (recordedSamples <= 0)
        {
            Debug.LogWarning("ExperimentFlow: Reflection recording captured no samples.");
            yield break;
        }

        SaveReflectionClip(recordedClip, recordedSamples, recordingLabel);
    }

    private void CacheReflectionPromptBaseText()
    {
        if (reflectionPromptText == null)
        {
            return;
        }

        reflectionPromptBaseText = reflectionPromptText.text;
    }

    private IEnumerator UpdateReflectionCountdown(float durationSeconds)
    {
        if (reflectionPromptText == null)
        {
            yield return new WaitForSeconds(durationSeconds);
            yield break;
        }

        float remainingTime = Mathf.Max(0f, durationSeconds);
        while (remainingTime > 0f)
        {
            int secondsRemaining = Mathf.CeilToInt(remainingTime);
            int minutes = secondsRemaining / 60;
            int seconds = secondsRemaining % 60;
            reflectionPromptText.text = $"{reflectionPromptBaseText}\n{minutes}:{seconds:00}";

            yield return null;
            remainingTime -= Time.deltaTime;
        }

        reflectionPromptText.text = $"{reflectionPromptBaseText}\n0:00";
    }

    private void ResetReflectionPromptText()
    {
        if (reflectionPromptText == null)
        {
            return;
        }

        reflectionPromptText.text = reflectionPromptBaseText;
    }

    private void SaveReflectionClip(AudioClip sourceClip, int recordedSamples, string recordingLabel)
    {
        if (sourceClip == null)
        {
            return;
        }

        int clampedSamples = Mathf.Clamp(recordedSamples, 1, sourceClip.samples);
        int totalSampleCount = clampedSamples * sourceClip.channels;
        float[] sampleBuffer = new float[totalSampleCount];
        sourceClip.GetData(sampleBuffer, 0);

        string safeParticipantId = SanitizeFileNameSegment(
            string.IsNullOrWhiteSpace(participantId) ? "participant" : participantId);
        string safeLabel = SanitizeFileNameSegment(
            string.IsNullOrWhiteSpace(recordingLabel) ? "reflection" : recordingLabel);
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string directoryPath = Path.Combine(Application.persistentDataPath, reflectionRecordingFolderName);
        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, $"{safeParticipantId}_{safeLabel}_{timestamp}.wav");
        WriteWavFile(filePath, sampleBuffer, sourceClip.channels, sourceClip.frequency);
        Debug.Log($"ExperimentFlow: Saved reflection recording to {filePath}");
    }

    private void WriteWavFile(string filePath, float[] samples, int channelCount, int sampleRate)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fileStream))
        {
            int bytesPerSample = 2;
            int byteRate = sampleRate * channelCount * bytesPerSample;
            int dataLength = samples.Length * bytesPerSample;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channelCount);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channelCount * bytesPerSample));
            writer.Write((short)(bytesPerSample * 8));
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            for (int i = 0; i < samples.Length; i++)
            {
                short pcmSample = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
                writer.Write(pcmSample);
            }
        }
    }

    private string SanitizeFileNameSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        StringBuilder builder = new StringBuilder(value.Length);
        char[] invalidCharacters = Path.GetInvalidFileNameChars();

        for (int i = 0; i < value.Length; i++)
        {
            char currentCharacter = value[i];
            bool isInvalid = false;

            for (int j = 0; j < invalidCharacters.Length; j++)
            {
                if (currentCharacter == invalidCharacters[j])
                {
                    isInvalid = true;
                    break;
                }
            }

            if (isInvalid || char.IsWhiteSpace(currentCharacter))
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(currentCharacter);
            }
        }

        return builder.ToString();
    }

    private void StartEndUIRoutine(IEnumerator routine)
    {
        Debug.LogWarning("ExperimentFlow: StartEndUIRoutine called.");
        if (endUIRoutine != null)
        {
            Debug.LogWarning("ExperimentFlow: Cancelling existing end UI routine before starting a new one.");
            StopCoroutine(endUIRoutine);
        }

        endUIRoutine = StartCoroutine(RunEndUIRoutine(routine));
    }

    private IEnumerator RunEndUIRoutine(IEnumerator routine)
    {
        Debug.LogWarning("ExperimentFlow: RunEndUIRoutine started.");
        yield return StartCoroutine(routine);
        Debug.LogWarning("ExperimentFlow: RunEndUIRoutine completed.");
        endUIRoutine = null;
    }

    private IEnumerator ShowEndUIAfterResponsiveSequence()
    {
        gardenController.StartResponsiveSequence();
        yield return new WaitUntil(() => gardenController == null || !gardenController.IsSequenceRunning);
        ShowEndUI();
    }

    private IEnumerator ShowEndUIAfterDelay(float delaySeconds)
    {
        Debug.LogWarning($"ExperimentFlow: Waiting {delaySeconds:0.##} seconds before showing EndUI.");
        yield return new WaitForSeconds(delaySeconds);
        Debug.LogWarning("ExperimentFlow: Delay complete. Showing EndUI now.");
        ShowEndUI();
    }

    private void ShowEndUI()
    {
        Debug.LogWarning("ExperimentFlow: ShowEndUI called.");
        StopAllHaptics();

        if (videoPhaseController != null)
        {
            videoPhaseController.ShowEndUI();
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: videoPhaseController is null, cannot show EndUI.");
        }
    }

    private void HideEndUI()
    {
        if (endUIRoutine != null)
        {
            Debug.LogWarning("ExperimentFlow: HideEndUI cancelled the pending end UI routine.");
            StopCoroutine(endUIRoutine);
            endUIRoutine = null;
        }

        Debug.LogWarning("ExperimentFlow: HideEndUI called.");
        if (videoPhaseController != null)
        {
            videoPhaseController.HideEndUI();
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: videoPhaseController is null, cannot hide EndUI.");
        }
    }
}
