using UnityEngine;

public class PacedGroundWaypointWalker : MonoBehaviour
{
    [Header("Path")]
    public Transform[] waypoints;
    public bool loop = true;
    public bool startAtClosestWaypoint = true;
    public bool lockYToStartHeight = true;

    [Header("Ground Following")]
    public bool followGround = false;
    public LayerMask groundLayers = ~0;
    public float groundRaycastHeight = 5f;
    public float groundRaycastDistance = 15f;
    public float groundOffset = 0f;
    public bool alignToGroundNormal = false;
    public float groundAlignSpeed = 8f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float turnSpeed = 4f;
    public float reachDistance = 0.2f;

    [Header("Pacing")]
    public float walkDuration = 5f;
    public float idleDuration = 3f;
    public bool startWalking = true;

    [Header("Animator")]
    public Animator animator;
    public string movingBoolParameter = "IsMoving";
    public string speedFloatParameter = "";
    public float animatorMoveSpeedValue = 1f;

    private int currentWaypointIndex;
    private float lockedY;
    private float phaseTimer;
    private bool isWalkingPhase;
    private bool initialized;

    private void Start()
    {
        lockedY = transform.position.y;
        isWalkingPhase = startWalking;
        phaseTimer = isWalkingPhase ? walkDuration : idleDuration;
        InitializePath();
        ApplyAnimatorState();
    }

    private void Update()
    {
        UpdatePhaseTimer();

        if (!HasValidPath())
        {
            SetAnimatorMoving(false);
            return;
        }

        if (!isWalkingPhase)
        {
            SetAnimatorMoving(false);
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            AdvanceWaypoint();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = targetWaypoint.position;

        if (followGround)
        {
            targetPosition.y = currentPosition.y;
        }
        else if (lockYToStartHeight)
        {
            targetPosition.y = lockedY;
        }

        Vector3 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget <= reachDistance)
        {
            AdvanceWaypoint();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        Vector3 flatDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
        Vector3 groundNormal = Vector3.up;
        bool hasGroundNormal = false;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

        if (followGround && TryGetGroundedPosition(nextPosition, out Vector3 groundedPosition, out groundNormal))
        {
            nextPosition = groundedPosition;
            hasGroundNormal = true;
        }

        transform.position = nextPosition;

        if (followGround && alignToGroundNormal && hasGroundNormal)
        {
            AlignRotationToGround(flatDirection, groundNormal);
        }

        SetAnimatorMoving(true);
    }

    private bool TryGetGroundedPosition(Vector3 position, out Vector3 groundedPosition, out Vector3 groundNormal)
    {
        Vector3 rayStart = position + Vector3.up * Mathf.Max(0f, groundRaycastHeight);
        float rayDistance = Mathf.Max(0.01f, groundRaycastHeight + groundRaycastDistance);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            groundedPosition = hit.point + hit.normal * groundOffset;
            groundNormal = hit.normal;
            return true;
        }

        groundedPosition = position;
        groundNormal = Vector3.up;
        return false;
    }

    private void AlignRotationToGround(Vector3 flatDirection, Vector3 groundNormal)
    {
        Vector3 forward = flatDirection.sqrMagnitude > 0.0001f
            ? Vector3.ProjectOnPlane(flatDirection, groundNormal).normalized
            : Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward, groundNormal);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, groundAlignSpeed * Time.deltaTime);
    }

    private void UpdatePhaseTimer()
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
        {
            return;
        }

        isWalkingPhase = !isWalkingPhase;
        phaseTimer = isWalkingPhase ? walkDuration : idleDuration;
        ApplyAnimatorState();
    }

    private void ApplyAnimatorState()
    {
        SetAnimatorMoving(isWalkingPhase);
    }

    private void InitializePath()
    {
        if (!HasValidPath())
        {
            initialized = false;
            return;
        }

        currentWaypointIndex = startAtClosestWaypoint ? FindClosestWaypointIndex() : 0;
        initialized = true;
    }

    private bool HasValidPath()
    {
        return waypoints != null && waypoints.Length > 0;
    }

    private int FindClosestWaypointIndex()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(transform.position - waypoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void AdvanceWaypoint()
    {
        if (!initialized || !HasValidPath() || waypoints.Length == 1)
        {
            return;
        }

        if (currentWaypointIndex < waypoints.Length - 1)
        {
            currentWaypointIndex++;
            return;
        }

        if (loop)
        {
            currentWaypointIndex = 0;
        }
    }

    private void SetAnimatorMoving(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(movingBoolParameter))
        {
            animator.SetBool(movingBoolParameter, isMoving);
        }

        if (!string.IsNullOrWhiteSpace(speedFloatParameter))
        {
            animator.SetFloat(speedFloatParameter, isMoving ? animatorMoveSpeedValue : 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform point = waypoints[i];
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawSphere(point.position, 0.15f);

            int nextIndex = i + 1;
            if (nextIndex >= waypoints.Length)
            {
                if (!loop)
                {
                    continue;
                }

                nextIndex = 0;
            }

            Transform nextPoint = waypoints[nextIndex];
            if (nextPoint != null)
            {
                Gizmos.DrawLine(point.position, nextPoint.position);
            }
        }
    }
}
