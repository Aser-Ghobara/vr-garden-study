using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoPhaseController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoGroup;
    public GameObject reflectionGroup;
    public GardenController gardenController;

    [Header("Head-Locked Video")]
    public Transform targetCamera;
    public float distanceFromCamera = 2f;
    public float verticalOffset = 0f;
    public float horizontalOffset = 0f;
    public Vector3 rotationOffsetEuler;

    [Header("Head-Locked Reflection")]
    public Transform reflectionFollowTarget;
    public float reflectionDistanceFromCamera = 1.5f;
    public float reflectionVerticalOffset = 0f;
    public float reflectionHorizontalOffset = 0f;
    public Vector3 reflectionRotationOffsetEuler;

    private void LateUpdate()
    {
        if (videoGroup == null || !videoGroup.activeInHierarchy || videoPlayer == null)
        {
            UpdateReflectionGroupHeadLock();
            return;
        }

        Transform cameraTransform = GetTargetCamera();
        if (cameraTransform == null)
        {
            UpdateReflectionGroupHeadLock();
            return;
        }

        Transform screenTransform = videoPlayer.transform;
        Vector3 desiredPosition =
            cameraTransform.position +
            cameraTransform.forward * distanceFromCamera +
            cameraTransform.up * verticalOffset +
            cameraTransform.right * horizontalOffset;

        screenTransform.position = desiredPosition;
        screenTransform.rotation =
            Quaternion.LookRotation(cameraTransform.position - desiredPosition, cameraTransform.up) *
            Quaternion.Euler(rotationOffsetEuler);

        UpdateReflectionGroupHeadLock();
    }

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

    private Transform GetTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private void UpdateReflectionGroupHeadLock()
    {
        if (reflectionGroup == null || !reflectionGroup.activeInHierarchy)
        {
            return;
        }

        Transform cameraTransform = GetTargetCamera();
        if (cameraTransform == null)
        {
            return;
        }

        Transform reflectionTransform = GetReflectionFollowTarget();
        if (reflectionTransform == null)
        {
            return;
        }

        Vector3 desiredPosition =
            cameraTransform.position +
            cameraTransform.forward * reflectionDistanceFromCamera +
            cameraTransform.up * reflectionVerticalOffset +
            cameraTransform.right * reflectionHorizontalOffset;

        reflectionTransform.position = desiredPosition;
        reflectionTransform.rotation =
            Quaternion.LookRotation(cameraTransform.position - desiredPosition, cameraTransform.up) *
            Quaternion.Euler(reflectionRotationOffsetEuler);
    }

    private Transform GetReflectionFollowTarget()
    {
        if (reflectionFollowTarget != null)
        {
            return reflectionFollowTarget;
        }

        if (reflectionGroup == null)
        {
            return null;
        }

        Canvas reflectionCanvas = reflectionGroup.GetComponentInChildren<Canvas>(true);
        if (reflectionCanvas != null)
        {
            return reflectionCanvas.transform;
        }

        return reflectionGroup.transform;
    }
}
