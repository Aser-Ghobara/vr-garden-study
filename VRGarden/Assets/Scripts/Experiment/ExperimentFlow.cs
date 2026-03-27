using System.Collections;
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
    [Tooltip("Plays during the recovery phase for responsive + haptic trials.")]
    public string responsiveRecoveryHapticEventName;

    [Tooltip("Plays 5 seconds after arriving in the garden for non-responsive + haptic trials.")]
    public string nonResponsiveGardenHapticEventName;

    private Coroutine startVideoRoutine;
    private Coroutine delayedHapticRoutine;
    private Coroutine endUIRoutine;

    private void Start()
    {
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

        if (Microphone.devices != null && Microphone.devices.Length > 0)
        {
            Microphone.Start(null, false, 20, 44100);
            yield return new WaitForSeconds(20f);

            if (Microphone.IsRecording(null))
            {
                Microphone.End(null);
            }
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: No microphone device found. Reflection recording skipped.");
            yield return new WaitForSeconds(20f);
        }

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
            gardenController.ConfigureRecoveryHaptics(null);
        }

        StopAllHaptics();

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        videoPhaseController.StartVideoPhase();
        yield return new WaitUntil(() => reflectionGroup != null && reflectionGroup.activeSelf);

        Debug.Log("ExperimentFlow: Starting 20-second reflection recording.");
        if (Microphone.devices != null && Microphone.devices.Length > 0)
        {
            Microphone.Start(null, false, 20, 44100);
            yield return new WaitForSeconds(20f);

            if (Microphone.IsRecording(null))
            {
                Microphone.End(null);
            }
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: No microphone device found. Reflection recording skipped.");
            yield return new WaitForSeconds(20f);
        }

        Debug.Log("ExperimentFlow: Reflection phase complete.");

        if (reflectionGroup != null)
        {
            reflectionGroup.SetActive(false);
        }

        if (transitionController != null)
        {
            Debug.Log("ExperimentFlow: Starting trial transition.");
            yield return StartCoroutine(transitionController.DoTransition());
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: TransitionController is not assigned. Activating garden group directly.");
            if (gardenGroup != null)
            {
                gardenGroup.SetActive(true);
            }
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
                gardenController.ConfigureRecoveryHaptics(responsiveRecoveryHapticEventName);
            }
        }
        else
        {
            if (gardenController != null)
            {
                gardenController.ConfigureRecoveryHaptics(null);
            }
        }

        if (trial.responsiveness == TrialManager.ResponsivenessType.NonResponsive)
        {
            if (trial.haptic == TrialManager.HapticType.Haptic)
            {
                StartNonResponsiveDelayedHaptic();
            }

            Debug.Log("Non-responsive trial: garden stays neutral.");
            StartEndUIRoutine(ShowEndUIAfterDelay(60f));
            yield break;
        }

        Debug.Log("Responsive trial: running garden sequence.");
        gardenController.StartResponsiveSequence();
        yield return new WaitUntil(() => gardenController == null || !gardenController.IsSequenceRunning);
        ShowEndUI();
    }

    private void StartNonResponsiveDelayedHaptic()
    {
        if (delayedHapticRoutine != null)
        {
            StopCoroutine(delayedHapticRoutine);
        }

        delayedHapticRoutine = StartCoroutine(PlayNonResponsiveHapticAfterDelay());
    }

    private IEnumerator PlayNonResponsiveHapticAfterDelay()
    {
        yield return new WaitForSeconds(5f);

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

        hapticsController.LoopHaptic(nonResponsiveGardenHapticEventName);
        delayedHapticRoutine = null;
    }

    private void StopAllHaptics()
    {
        if (delayedHapticRoutine != null)
        {
            StopCoroutine(delayedHapticRoutine);
            delayedHapticRoutine = null;
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
