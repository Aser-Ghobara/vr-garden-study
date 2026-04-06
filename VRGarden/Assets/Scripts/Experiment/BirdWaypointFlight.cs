using UnityEngine;

public class BirdWaypointFlight : MonoBehaviour
{
    [Header("Path")]
    public Transform[] waypoints;
    public bool loop = true;
    public bool startAtClosestWaypoint = true;

    [Header("Movement")]
    public float speed = 3f;
    public float turnSpeed = 4f;
    public float reachDistance = 0.5f;

    [Header("Flight Motion")]
    public bool enableBobbing = true;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 1.25f;

    private int currentWaypointIndex;
    private Vector3 basePosition;
    private bool initialized;

    private void Start()
    {
        InitializePath();
    }

    private void Update()
    {
        if (!HasValidPath())
        {
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            AdvanceWaypoint();
            return;
        }

        Vector3 targetPosition = targetWaypoint.position;
        Vector3 horizontalDirection = targetPosition - basePosition;

        if (horizontalDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        basePosition = Vector3.MoveTowards(basePosition, targetPosition, speed * Time.deltaTime);

        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = basePosition + Vector3.up * bobOffset;
        }
        else
        {
            transform.position = basePosition;
        }

        if (Vector3.Distance(basePosition, targetPosition) <= reachDistance)
        {
            AdvanceWaypoint();
        }
    }

    private void InitializePath()
    {
        basePosition = transform.position;

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
            if (waypoints[i] == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(transform.position - waypoints[i].position);
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
        if (!initialized || !HasValidPath())
        {
            return;
        }

        if (waypoints.Length == 1)
        {
            return;
        }

        currentWaypointIndex++;

        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = loop ? 0 : waypoints.Length - 1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform point = waypoints[i];
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawSphere(point.position, 0.2f);

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
