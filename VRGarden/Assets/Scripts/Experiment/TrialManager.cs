using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TrialManager : MonoBehaviour
{
    // Attach this to an empty scene object, e.g., "ExperimentManager".
    // Assign the ExperimentFlow reference in the Inspector.
    // Create scene objects: VideoScreen (+ VideoPlayer), PromptCanvas (+ Text), GardenRoot (+ GardenController).

    public enum EmotionType
    {
        Happy,
        Sad
    }

    public enum ResponsivenessType
    {
        Responsive,
        NonResponsive
    }

    [System.Serializable]
    public class Trial
    {
        public EmotionType emotion;
        public ResponsivenessType responsiveness;
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

    private void Start()
    {
        SetupTrials();
        StartCoroutine(RunNextTrial());
    }

    public void SetupTrials()
    {
        trials.Clear();

        // Four conditions:
        // 1) Responsive + Happy
        // 2) Responsive + Sad
        // 3) NonResponsive + Happy
        // 4) NonResponsive + Sad
        trials.Add(new Trial
        {
            trialIndex = 0,
            responsiveness = ResponsivenessType.Responsive,
            emotion = EmotionType.Happy,
            videoClip = happyClip
        });

        trials.Add(new Trial
        {
            trialIndex = 1,
            responsiveness = ResponsivenessType.Responsive,
            emotion = EmotionType.Sad,
            videoClip = sadClip
        });

        trials.Add(new Trial
        {
            trialIndex = 2,
            responsiveness = ResponsivenessType.NonResponsive,
            emotion = EmotionType.Happy,
            videoClip = happyClip
        });

        trials.Add(new Trial
        {
            trialIndex = 3,
            responsiveness = ResponsivenessType.NonResponsive,
            emotion = EmotionType.Sad,
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
            Debug.Log($"Starting trial {current.trialIndex}: {current.responsiveness} + {current.emotion}");

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
}
