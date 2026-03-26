using TMPro;
using UnityEngine;

/// <summary>
/// Shows a text prompt that guides the user to look toward a target direction.
///
/// Basic setup:
/// 1. Create a Canvas with a TextMeshProUGUI label.
/// 2. Add this component to the Canvas or another manager object.
/// 3. Assign the prompt text and the user's camera transform.
/// 4. Assign either a target transform or a manual world direction.
///
/// Typical usage:
/// - Call ShowPrompt() to begin guiding the user.
/// - The prompt hides itself once the user is facing the target within the allowed angle.
/// - Call HidePrompt() to force-hide it.
/// </summary>
public class LookDirectionPrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform userCamera;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject promptRoot;

    [Header("Target")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 manualWorldDirection = Vector3.forward;
    [SerializeField] private bool useTargetTransform = true;

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Please look at the indicated direction.";
    [SerializeField] private float allowedAngle = 20f;
    [SerializeField] private bool hideWhenAligned = true;
    [SerializeField] private bool updatePromptWithDirectionHint = true;

    [Header("Optional Head Lock")]
    [SerializeField] private bool followCamera = false;
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private float verticalOffset = -0.15f;
    [SerializeField] private float horizontalOffset = 0f;

    private bool isActive;

    private void Awake()
    {
        if (promptRoot == null && promptText != null)
        {
            promptRoot = promptText.gameObject;
        }

        HidePrompt();
    }

    private void LateUpdate()
    {
        if (!isActive)
        {
            return;
        }

        Transform cameraTransform = GetUserCamera();
        if (cameraTransform == null)
        {
            return;
        }

        if (followCamera && promptRoot != null)
        {
            Vector3 desiredPosition =
                cameraTransform.position +
                cameraTransform.forward * distanceFromCamera +
                cameraTransform.up * verticalOffset +
                cameraTransform.right * horizontalOffset;

            promptRoot.transform.position = desiredPosition;
            promptRoot.transform.rotation = Quaternion.LookRotation(promptRoot.transform.position - cameraTransform.position, cameraTransform.up);
        }

        Vector3 targetDirection = GetTargetDirection(cameraTransform);
        if (targetDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Vector3.Angle(cameraTransform.forward, targetDirection);

        if (updatePromptWithDirectionHint && promptText != null)
        {
            promptText.text = BuildPromptMessage(cameraTransform, targetDirection, angle);
        }

        if (hideWhenAligned && angle <= allowedAngle)
        {
            HidePrompt();
        }
    }

    public void ShowPrompt()
    {
        isActive = true;

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
    }

    public void HidePrompt()
    {
        isActive = false;

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        useTargetTransform = newTarget != null;
    }

    public void SetManualDirection(Vector3 worldDirection)
    {
        manualWorldDirection = worldDirection.normalized;
        useTargetTransform = false;
    }

    public bool IsAligned()
    {
        Transform cameraTransform = GetUserCamera();
        if (cameraTransform == null)
        {
            return false;
        }

        Vector3 targetDirection = GetTargetDirection(cameraTransform);
        if (targetDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        return Vector3.Angle(cameraTransform.forward, targetDirection) <= allowedAngle;
    }

    private Transform GetUserCamera()
    {
        if (userCamera != null)
        {
            return userCamera;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private Vector3 GetTargetDirection(Transform cameraTransform)
    {
        if (useTargetTransform && targetTransform != null)
        {
            Vector3 toTarget = targetTransform.position - cameraTransform.position;
            toTarget.y = 0f;
            return toTarget.normalized;
        }

        Vector3 direction = manualWorldDirection;
        direction.y = 0f;
        return direction.normalized;
    }

    private string BuildPromptMessage(Transform cameraTransform, Vector3 targetDirection, float angle)
    {
        if (angle <= allowedAngle)
        {
            return promptMessage;
        }

        Vector3 localDirection = cameraTransform.InverseTransformDirection(targetDirection);
        string hint;

        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
        {
            hint = localDirection.x > 0f ? "Look right" : "Look left";
        }
        else
        {
            hint = localDirection.z > 0f ? "Look forward" : "Turn around";
        }

        return $"{promptMessage}\n{hint}";
    }
}
