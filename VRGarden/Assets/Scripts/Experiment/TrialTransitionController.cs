using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrialTransitionController : MonoBehaviour
{
    public GameObject xrOrigin;
    public Transform gardenSpawnPoint;
    public Transform cabinSpawnPoint;
    public CanvasGroup fadeCanvasGroup;
    public GardenController gardenController;
    public bool preferRuntimeCameraFadeOverlay = true;
    public float runtimeFadeOverlayDistance = 0.15f;
    public float runtimeFadeOverlayMargin = 1.25f;

    private CanvasGroup activeFadeCanvasGroup;
    private CanvasGroup runtimeFadeCanvasGroup;
    private Canvas runtimeFadeCanvas;
    private RectTransform runtimeFadeCanvasRect;

    private void Awake()
    {
        RefreshActiveFadeCanvasGroup();

        if (activeFadeCanvasGroup != null)
        {
            activeFadeCanvasGroup.alpha = 0f;
            activeFadeCanvasGroup.gameObject.SetActive(false);
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
        RefreshActiveFadeCanvasGroup();

        if (activeFadeCanvasGroup == null)
        {
            Debug.LogWarning("TrialTransitionController: No fade canvas is available.");
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

        float fadeDuration = 0.6f;
        UpdateRuntimeFadeCanvasPlacement();
        activeFadeCanvasGroup.gameObject.SetActive(true);
        activeFadeCanvasGroup.alpha = 0f;
        yield return StartCoroutine(FadeCanvasAlpha(0f, 1f, fadeDuration));
        xrOrigin.transform.position = gardenSpawnPoint.position;

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

        yield return StartCoroutine(FadeCanvasAlpha(1f, 0f, fadeDuration));

        activeFadeCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasAlpha(float startAlpha, float endAlpha, float duration)
    {
        RefreshActiveFadeCanvasGroup();

        if (activeFadeCanvasGroup == null)
        {
            yield break;
        }

        UpdateRuntimeFadeCanvasPlacement();
        activeFadeCanvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        float clampedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < clampedDuration)
        {
            float t = elapsed / clampedDuration;
            activeFadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        activeFadeCanvasGroup.alpha = endAlpha;
    }

    private void RefreshActiveFadeCanvasGroup()
    {
        if (preferRuntimeCameraFadeOverlay && EnsureRuntimeFadeCanvas())
        {
            activeFadeCanvasGroup = runtimeFadeCanvasGroup;
            return;
        }

        activeFadeCanvasGroup = fadeCanvasGroup;
    }

    private bool EnsureRuntimeFadeCanvas()
    {
        if (runtimeFadeCanvasGroup != null)
        {
            UpdateRuntimeFadeCanvasPlacement();
            return true;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return false;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
        {
            uiLayer = 0;
        }

        GameObject canvasObject = new GameObject("RuntimeTransitionFadeCanvas");
        canvasObject.layer = uiLayer;
        canvasObject.transform.SetParent(targetCamera.transform, false);

        runtimeFadeCanvas = canvasObject.AddComponent<Canvas>();
        runtimeFadeCanvas.renderMode = RenderMode.WorldSpace;
        runtimeFadeCanvas.worldCamera = targetCamera;
        runtimeFadeCanvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<GraphicRaycaster>();
        runtimeFadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        runtimeFadeCanvasRect = canvasObject.GetComponent<RectTransform>();
        runtimeFadeCanvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        runtimeFadeCanvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        runtimeFadeCanvasRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject imageObject = new GameObject("RuntimeTransitionFadeOverlay");
        imageObject.layer = uiLayer;
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        UpdateRuntimeFadeCanvasPlacement();
        runtimeFadeCanvasGroup.alpha = 0f;
        runtimeFadeCanvasGroup.gameObject.SetActive(false);
        return true;
    }

    private void UpdateRuntimeFadeCanvasPlacement()
    {
        if (runtimeFadeCanvasRect == null || runtimeFadeCanvas == null)
        {
            return;
        }

        Camera targetCamera = runtimeFadeCanvas.worldCamera != null ? runtimeFadeCanvas.worldCamera : Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        float distance = Mathf.Max(0.05f, runtimeFadeOverlayDistance);
        float margin = Mathf.Max(1.05f, runtimeFadeOverlayMargin);
        float height = 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance * margin;
        float width = height * targetCamera.aspect;

        runtimeFadeCanvasRect.localPosition = new Vector3(0f, 0f, distance);
        runtimeFadeCanvasRect.localRotation = Quaternion.identity;
        runtimeFadeCanvasRect.localScale = Vector3.one * 0.001f;
        runtimeFadeCanvasRect.sizeDelta = new Vector2(width * 1000f, height * 1000f);
    }
}
