using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class VideoPhaseController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoGroup;
    public GameObject reflectionGroup;
    public GameObject endUIGroup;
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

    [Header("Head-Locked End UI")]
    public Transform endUIFollowTarget;
    public float endUIDistanceFromCamera = 1.5f;
    public float endUIVerticalOffset = 0f;
    public float endUIHorizontalOffset = 0f;
    public Vector3 endUIRotationOffsetEuler;

    private void LateUpdate()
    {
        if (videoGroup == null || !videoGroup.activeInHierarchy || videoPlayer == null)
        {
            UpdateReflectionGroupHeadLock();
            UpdateEndUIHeadLock();
            return;
        }

        Transform cameraTransform = GetTargetCamera();
        if (cameraTransform == null)
        {
            UpdateReflectionGroupHeadLock();
            UpdateEndUIHeadLock();
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
        UpdateEndUIHeadLock();
    }

    public void StartVideoPhase()
    {
        HideEndUI();

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

    public void ShowEndUI()
    {
        if (endUIGroup != null)
        {
            endUIGroup.SetActive(true);
        }
    }

    public void HideEndUI()
    {
        if (endUIGroup != null)
        {
            endUIGroup.SetActive(false);
        }
    }

    private void UpdateReflectionGroupHeadLock()
    {
        UpdateHeadLockedGroup(
            reflectionGroup,
            GetReflectionFollowTarget(),
            reflectionDistanceFromCamera,
            reflectionVerticalOffset,
            reflectionHorizontalOffset,
            reflectionRotationOffsetEuler);
    }

    private void UpdateEndUIHeadLock()
    {
        UpdateHeadLockedGroup(
            endUIGroup,
            GetEndUIFollowTarget(),
            endUIDistanceFromCamera,
            endUIVerticalOffset,
            endUIHorizontalOffset,
            endUIRotationOffsetEuler);
    }

    private void UpdateHeadLockedGroup(
        GameObject group,
        Transform followTarget,
        float distanceFromTarget,
        float verticalOffsetAmount,
        float horizontalOffsetAmount,
        Vector3 rotationOffset)
    {
        if (group == null || !group.activeInHierarchy)
        {
            return;
        }

        Transform cameraTransform = GetTargetCamera();
        if (cameraTransform == null || followTarget == null)
        {
            return;
        }

        Vector3 desiredPosition =
            cameraTransform.position +
            cameraTransform.forward * distanceFromTarget +
            cameraTransform.up * verticalOffsetAmount +
            cameraTransform.right * horizontalOffsetAmount;

        followTarget.position = desiredPosition;
        followTarget.rotation =
            Quaternion.LookRotation(cameraTransform.position - desiredPosition, cameraTransform.up) *
            Quaternion.Euler(rotationOffset);
    }

    private Transform GetReflectionFollowTarget()
    {
        return GetFollowTarget(reflectionGroup, reflectionFollowTarget);
    }

    private Transform GetEndUIFollowTarget()
    {
        if (endUIFollowTarget != null)
        {
            return endUIFollowTarget;
        }

        if (endUIGroup == null)
        {
            return null;
        }

        return endUIGroup.transform;
    }

    private Transform GetFollowTarget(GameObject group, Transform explicitTarget)
    {
        if (explicitTarget != null)
        {
            return explicitTarget;
        }

        if (group == null)
        {
            return null;
        }

        TMP_Text groupText = group.GetComponentInChildren<TMP_Text>(true);
        if (groupText != null)
        {
            return groupText.transform;
        }

        Canvas groupCanvas = group.GetComponentInChildren<Canvas>(true);
        if (groupCanvas != null)
        {
            return groupCanvas.transform;
        }

        return group.transform;
    }
}
