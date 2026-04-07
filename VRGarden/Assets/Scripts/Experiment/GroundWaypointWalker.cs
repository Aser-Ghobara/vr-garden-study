using UnityEngine;

public class GroundWaypointWalker : MonoBehaviour
{
    [Header("Path")]
    public Transform[] waypoints;
    public bool loop = true;
    public bool startAtClosestWaypoint = true;
    public bool lockYToStartHeight = true;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float turnSpeed = 4f;
    public float reachDistance = 0.2f;

    [Header("Animator")]
    public Animator animator;
    public string movingBoolParameter = "IsMoving";
    public string speedFloatParameter = "";
    public float animatorMoveSpeedValue = 1f;

    [Header("Movement Control")]
    public bool startMovingOnPlay = true;

    private int currentWaypointIndex;
    private float lockedY;
    private bool initialized;
    private bool movementEnabled;

    private void Start()
    {
        lockedY = transform.position.y;
        movementEnabled = startMovingOnPlay;
        InitializePath();
    }

    private void Update()
    {
        if (!HasValidPath())
        {
            SetAnimatorMoving(false);
            return;
        }

        if (!movementEnabled)
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

        if (lockYToStartHeight)
        {
            targetPosition.y = lockedY;
        }

        Vector3 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget <= reachDistance)
        {
            bool willContinueMoving = loop || currentWaypointIndex < waypoints.Length - 1;
            SetAnimatorMoving(willContinueMoving);
            AdvanceWaypoint();
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        Vector3 flatDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
        SetAnimatorMoving(true);
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
        SetAnimatorMoving(movementEnabled);
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
        else
        {
            SetAnimatorMoving(false);
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

    public void SetMovementEnabled(bool isEnabled)
    {
        movementEnabled = isEnabled;
        SetAnimatorMoving(isEnabled);
    }

    public void StartMoving()
    {
        SetMovementEnabled(true);
    }

    public void StopMoving()
    {
        SetMovementEnabled(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.green;

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
