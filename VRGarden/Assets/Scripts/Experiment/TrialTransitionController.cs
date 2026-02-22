using System.Collections;
using UnityEngine;

public class TrialTransitionController : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform gardenSpawnPoint;
    public CanvasGroup fadeCanvasGroup;
    public GardenController gardenController;

    private void Awake()
    {
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    public void StartGardenTransition()
    {
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
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

        gardenController.StartNonResponsiveSequence();
    }
}