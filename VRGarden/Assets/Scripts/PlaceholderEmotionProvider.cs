using System;
using System.Collections;
using UnityEngine;

public enum DetectedEmotion
{
    Happy,
    Sad
}

public class PlaceholderEmotionProvider : MonoBehaviour
{
    public DetectedEmotion forcedEmotion = DetectedEmotion.Happy;
    public float simulatedDelaySeconds = 0.5f;

    public IEnumerator GetDetectedEmotion(Action<DetectedEmotion> onComplete)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, simulatedDelaySeconds));
        onComplete?.Invoke(forcedEmotion);
    }
}
