using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class ExperimentFlow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the TrialManager in the Inspector (usually on your ExperimentManager object).")]
    public TrialManager trialManager;

    [Tooltip("Assign the VideoPlayer used to present each trial clip (on your video screen object).")]
    public VideoPlayer videoPlayer;

    [Tooltip("Assign the world-space reflection prompt Canvas GameObject.")]
    public GameObject promptCanvas;

    [Tooltip("Assign the GardenController component on your garden root object.")]
    public GardenController gardenController;

    [Header("Durations (seconds)")]
    public float reflectionDuration = 30f;
    public float gardenDuration = 60f;

    private bool videoFinished;

    private void Start()
    {
        StartCoroutine(RunAllTrials());
    }

    private IEnumerator RunAllTrials()
    {
        if (trialManager == null)
        {
            Debug.LogError("ExperimentFlow: TrialManager is not assigned.");
            yield break;
        }

        if (trialManager.trials == null || trialManager.trials.Count == 0)
        {
            trialManager.SetupTrials();
        }

        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }

        if (gardenController != null)
        {
            gardenController.gameObject.SetActive(false);
        }

        for (int i = 0; i < trialManager.trials.Count; i++)
        {
            yield return StartCoroutine(RunTrial(trialManager.trials[i]));
        }
    }

    public IEnumerator RunTrial(TrialManager.Trial trial)
    {
        if (trial == null)
        {
            yield break;
        }

        if (videoPlayer != null && trial.videoClip != null)
        {
            videoFinished = false;
            videoPlayer.loopPointReached += OnVideoFinished;

            videoPlayer.clip = trial.videoClip;
            videoPlayer.Play();

            // Wait until playback has reached the end; fallback loop also checks isPlaying.
            yield return new WaitUntil(() => videoFinished || !videoPlayer.isPlaying);

            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        else
        {
            Debug.LogWarning("ExperimentFlow: Missing VideoPlayer or trial VideoClip.");
        }

        if (promptCanvas != null)
        {
            promptCanvas.SetActive(true);
            yield return new WaitForSeconds(reflectionDuration);
            promptCanvas.SetActive(false);
        }

        if (gardenController == null)
        {
            Debug.LogWarning("ExperimentFlow: GardenController is not assigned.");
            yield break;
        }

        gardenController.gameObject.SetActive(true);

        if (trial.responsiveness == TrialManager.ResponsivenessType.Responsive)
        {
            float valence = trial.emotion == TrialManager.EmotionType.Happy ? 1f : -1f;
            float intensity = 0.8f;
            gardenController.RunResponsiveGarden(valence, intensity);
        }
        else
        {
            gardenController.RunNonResponsiveGarden();
        }

        yield return new WaitForSeconds(gardenDuration);
        gardenController.gameObject.SetActive(false);
    }

    // Compatibility wrapper for existing TrialManager calls.
    public IEnumerator RunTrialSequence(TrialManager.Trial trial)
    {
        yield return RunTrial(trial);
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        videoFinished = true;
    }
}
