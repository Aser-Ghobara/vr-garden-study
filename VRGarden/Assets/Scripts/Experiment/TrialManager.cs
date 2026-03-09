using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TrialManager : MonoBehaviour
{
    // Attach this to an empty scene object, e.g., "ExperimentManager".
    // Assign the ExperimentFlow reference in the Inspector.
    // Create scene objects: VideoScreen (+ VideoPlayer), PromptCanvas (+ Text), GardenRoot (+ GardenController).

    public enum ResponsivenessType
    {
        Responsive,
        NonResponsive
    }

    public enum HapticType
    {
        Haptic,
        NoHaptic
    }

    [System.Serializable]
    public class Trial
    {
        public ResponsivenessType responsiveness;
        public HapticType haptic;
        public VideoClip videoClip;
        public int trialIndex;
    }

    [Header("Trial Data")]
    public List<Trial> trials = new List<Trial>();

    [Header("Optional clip defaults")]
    public VideoClip happyClip;
    public VideoClip sadClip;

    [Header("Flow")]
    public ExperimentFlow experimentFlow;
    public TrialTransitionController transitionController;

    private void Start()
    {
        SetupTrials();
        // StartCoroutine(RunNextTrial());
    }

    public void SetupTrials()
    {
        trials.Clear();

        trials.Add(new Trial
        {
            trialIndex = 0,
            responsiveness = ResponsivenessType.Responsive,
            haptic = HapticType.Haptic,
            videoClip = sadClip
        });

        trials.Add(new Trial
        {
            trialIndex = 1,
            responsiveness = ResponsivenessType.Responsive,
            haptic = HapticType.NoHaptic,
            videoClip = sadClip
        });

        trials.Add(new Trial
        {
            trialIndex = 2,
            responsiveness = ResponsivenessType.NonResponsive,
            haptic = HapticType.Haptic,
            videoClip = sadClip
        });

        trials.Add(new Trial
        {
            trialIndex = 3,
            responsiveness = ResponsivenessType.NonResponsive,
            haptic = HapticType.NoHaptic,
            videoClip = sadClip
        });
    }

    private IEnumerator RunNextTrial()
    {
        // Placeholder trial loop.
        // Extend this to randomize order, counterbalance, log data, and handle user input/events.
        for (int i = 0; i < trials.Count; i++)
        {
            Trial current = trials[i];
            Debug.Log($"Starting trial {current.trialIndex}: {current.responsiveness} + {current.haptic}");

            if (experimentFlow != null)
            {
                yield return StartCoroutine(experimentFlow.RunTrialSequence(current));
            }
            else
            {
                Debug.LogWarning("ExperimentFlow is not assigned on TrialManager.");
                yield return null;
            }
        }

        Debug.Log("All trials complete.");
    }
    public void RunTrialByIndex(int index)
{
    if (index < 0 || index >= trials.Count)
    {
        Debug.LogWarning("Invalid trial index.");
        return;
    }

    StopAllCoroutines(); // stop any running trials

    Trial selected = trials[index];
    Debug.Log($"Manually starting trial {selected.trialIndex}: {selected.responsiveness} + {selected.haptic}");

    if (transitionController != null)
    {
        transitionController.TeleportToCabin();
    }

    StartCoroutine(experimentFlow.RunTrialSequence(selected));
}
}
