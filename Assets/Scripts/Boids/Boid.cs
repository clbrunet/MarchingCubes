using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private const float RAYS_OFFSET_ANGLE = 45f;
    private const float VISION_RADIUS = 1.5f;
    private const float WALL_RAYCAST_DISTANCE = 2.5f;
    private const float SPEED = 2f;
    private const float ROTATE_SPEED = SPEED * 100f * Mathf.Deg2Rad;

    [SerializeField]
    private LayerMask raycastLayer;
    private readonly Vector3[] raysDirections =
    {
        Quaternion.Euler(0f, -RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(0f, RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
        Quaternion.Euler(-RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
    };
    private Vector3 targetForward;

    private void Start()
    {
        targetForward = transform.forward;
    }

    private void Update()
    {
        BoidAI();
        AvoidWalls();
        transform.forward = Vector3.RotateTowards(transform.forward, targetForward, ROTATE_SPEED * Time.deltaTime, 0f);
        transform.Translate(SPEED * Time.deltaTime * Vector3.forward);
    }

    private void BoidAI()
    {
        Vector3 separationPositionsSum = Vector3.zero;
        int separationPositionsCount = 0;
        Vector3 alignmentTargetForwardsSum = Vector3.zero;
        int alignmentTargetForwardsCount = 0;
        Vector3 cohesionPositionsSum = Vector3.zero;
        int cohesionPositionsCount = 0;
        foreach (Boid boid in BoidsManager.Instance.boids)
        {
            if (boid == this || Vector3.Distance(transform.position, boid.transform.position) > VISION_RADIUS)
            {
                continue;
            }
            alignmentTargetForwardsSum += boid.targetForward;
            alignmentTargetForwardsCount++;
            cohesionPositionsSum += boid.transform.position;
            cohesionPositionsCount++;
            if (Vector3.Distance(transform.position, boid.transform.position) < VISION_RADIUS / 2)
            {
                separationPositionsSum += boid.transform.position;
                separationPositionsCount++;
            }
        }
        targetForward = targetForward.normalized;
        Vector3 separation = targetForward;
        Vector3 alignment = targetForward;
        Vector3 cohesion = targetForward;
        if (separationPositionsCount != 0)
        {
            separation = (transform.position - (separationPositionsSum / separationPositionsCount)).normalized;
        }
        if (alignmentTargetForwardsCount != 0)
        {
            alignment = (alignmentTargetForwardsSum / alignmentTargetForwardsCount).normalized;
        }
        if (cohesionPositionsCount != 0)
        {
            cohesion = ((cohesionPositionsSum / cohesionPositionsCount) - transform.position).normalized;
        }
        targetForward = Vector3.RotateTowards(targetForward,
            (targetForward + separation * 2 + alignment + cohesion) / 5, ROTATE_SPEED / 2 * Time.deltaTime, 0f);
    }

    private void AvoidWalls()
    {
        if (Physics.Raycast(transform.position, targetForward, WALL_RAYCAST_DISTANCE, raycastLayer))
        {
            List<Vector3> possibleDirections = new();
            foreach (Vector3 rayDirection in raysDirections)
            {
                Vector3 direction = transform.rotation * rayDirection;
                if (!Physics.Raycast(transform.position, direction, WALL_RAYCAST_DISTANCE, raycastLayer))
                {
                    possibleDirections.Add(direction);
                }
            }
            if (possibleDirections.Count == 0)
            {
                targetForward = -transform.forward;
            }
            else
            {
                targetForward = possibleDirections[Random.Range(0, possibleDirections.Count)];
            }
        }
    }
}
