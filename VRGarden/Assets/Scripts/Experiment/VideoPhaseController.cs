using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoPhaseController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoGroup;
    public GameObject reflectionGroup;
    public GardenController gardenController;

    public void StartVideoPhase()
    {
        if (gardenController != null && gardenController.ambienceSource != null)
        {
            gardenController.ambienceSource.Stop();
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("VideoPhaseController: videoPlayer is not assigned.");
            return;
        }

        if (videoGroup == null)
        {
            Debug.LogWarning("VideoPhaseController: videoGroup is not assigned.");
            return;
        }

        if (reflectionGroup == null)
        {
            Debug.LogWarning("VideoPhaseController: reflectionGroup is not assigned.");
            return;
        }

        videoGroup.SetActive(true);
        reflectionGroup.SetActive(false);
        videoPlayer.Play();

        StartCoroutine(WaitForVideoEnd());
    }

    private IEnumerator WaitForVideoEnd()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("VideoPhaseController: videoPlayer is not assigned.");
            yield break;
        }

        if (videoGroup == null)
        {
            Debug.LogWarning("VideoPhaseController: videoGroup is not assigned.");
            yield break;
        }

        if (reflectionGroup == null)
        {
            Debug.LogWarning("VideoPhaseController: reflectionGroup is not assigned.");
            yield break;
        }

        while (!videoPlayer.isPlaying)
        {
            yield return null;
        }

        while (videoPlayer.isPlaying)
        {
            yield return null;
        }

        videoGroup.SetActive(false);
        reflectionGroup.SetActive(true);
    }
}
