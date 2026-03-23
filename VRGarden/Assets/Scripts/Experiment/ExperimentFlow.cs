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
    public HapticVestController hapticVestController;
    public ExternalHapticsController externalHapticsController;
    public VideoPhaseController videoPhaseController;

    public TrialTransitionController transitionController;

    [Tooltip("Assign the happy clip to play when StartVideo is pressed.")]
    public VideoClip happyVideoClip;

    [Tooltip("Optional: assign the Start Garden button to enable it after reflection recording.")]
    public Button startGardenButton;

    private Coroutine startVideoRoutine;

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

    }

    public void StartVideo()
    {
        if (startVideoRoutine != null)
        {
            StopCoroutine(startVideoRoutine);
        }

        startVideoRoutine = StartCoroutine(StartVideoRoutine());
    }

    public void StartGarden()
    {
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

        gardenController.StartResponsiveSequence();
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

        if (startVideoRoutine != null)
        {
            StopCoroutine(startVideoRoutine);
            startVideoRoutine = null;
        }

        if (gardenController != null)
        {
            gardenController.ResetGardenToNeutral();
        }

        StopExternalHaptics();

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

        if (trial.haptic == TrialManager.HapticType.Haptic)
        {
            Debug.Log("Haptic trial: starting external haptics.");
            StartExternalHaptics();
        }
        else
        {
            Debug.Log("No-haptic trial: external haptics remain stopped.");
            StopExternalHaptics();
        }

        if (trial.responsiveness == TrialManager.ResponsivenessType.NonResponsive)
        {
            Debug.Log("Non-responsive trial: garden stays neutral.");
            yield break;
        }

        Debug.Log("Responsive trial: running garden sequence.");
        gardenController.StartResponsiveSequence();
    }

    private void StartExternalHaptics()
    {
        if (externalHapticsController != null)
        {
            externalHapticsController.StartHapticsExample();
            return;
        }

        Debug.LogWarning("ExperimentFlow: ExternalHapticsController is not assigned.");
    }

    private void StopExternalHaptics()
    {
        if (externalHapticsController != null)
        {
            externalHapticsController.StopHaptics();
        }
    }
}
