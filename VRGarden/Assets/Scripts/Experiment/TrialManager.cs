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

    private void OnValidate()
    {
        SetupTrials();
    }

    public void SetupTrials()
    {
        if (trials == null)
        {
            trials = new List<Trial>();
        }

        trials.RemoveAll(existingTrial => existingTrial == null);

        EnsureTrial(0, ResponsivenessType.Responsive, HapticType.Haptic);
        EnsureTrial(1, ResponsivenessType.Responsive, HapticType.NoHaptic);
        EnsureTrial(2, ResponsivenessType.NonResponsive, HapticType.Haptic);
        EnsureTrial(3, ResponsivenessType.NonResponsive, HapticType.NoHaptic);
        trials.Sort((left, right) => left.trialIndex.CompareTo(right.trialIndex));
    }

    private void EnsureTrial(int trialIndex, ResponsivenessType responsiveness, HapticType haptic)
    {
        Trial trial = trials.Find(existingTrial => existingTrial.trialIndex == trialIndex);
        if (trial == null)
        {
            trial = new Trial
            {
                trialIndex = trialIndex,
                videoClip = sadClip
            };

            trials.Add(trial);
        }

        trial.responsiveness = responsiveness;
        trial.haptic = haptic;
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

        if (experimentFlow != null)
        {
            StartCoroutine(experimentFlow.RunTrialSequence(selected));
        }
        else
        {
            Debug.LogWarning("ExperimentFlow is not assigned on TrialManager.");
        }
    }
}
