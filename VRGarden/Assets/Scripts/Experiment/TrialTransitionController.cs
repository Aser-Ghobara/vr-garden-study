using System.Collections;
using UnityEngine;

public class TrialTransitionController : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform gardenSpawnPoint;
    public Transform cabinSpawnPoint;
    public CanvasGroup fadeCanvasGroup;
    public GardenController gardenController;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    public void StartGardenTransition()
    {
        StartCoroutine(DoTransition());
    }

    public void TeleportToCabin()
    {
        if (xrOrigin == null || cabinSpawnPoint == null)
        {
            Debug.LogWarning("TeleportToCabin: missing xrOrigin or cabinSpawnPoint.");
            return;
        }

        xrOrigin.transform.position = cabinSpawnPoint.position;
    }

    public IEnumerator DoTransition()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("TrialTransitionController: fadeCanvasGroup is not assigned.");
            yield break;
        }

        if (xrOrigin == null)
        {
            Debug.LogWarning("TrialTransitionController: xrOrigin is not assigned.");
            yield break;
        }

        if (gardenSpawnPoint == null)
        {
            Debug.LogWarning("TrialTransitionController: gardenSpawnPoint is not assigned.");
            yield break;
        }

        float fadeDuration = 1.2f;
        float elapsed = 0f;

        fadeCanvasGroup.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(0.3f);
        xrOrigin.transform.position = gardenSpawnPoint.position;

        yield return new WaitForSeconds(0.3f);

        // Re-enable jungle ambience before season escalation begins.
        if (gardenController != null && gardenController.ambienceSource != null && gardenController.jungleClip != null)
        {
            gardenController.ambienceSource.Stop();
            gardenController.ambienceSource.clip = gardenController.jungleClip;
            gardenController.ambienceSource.loop = true;
            gardenController.ambienceSource.volume = 0.05f;
            gardenController.ambienceSource.Play();
        }
        else
        {
            Debug.LogWarning("TrialTransitionController: Missing GardenController ambienceSource or jungleClip for transition ambience.");
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        fadeCanvasGroup.gameObject.SetActive(false);
    }
}
