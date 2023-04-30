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
        Vector3 separation = GetSeparationVector();
        Vector3 alignment = GetAlignmentVector();
        Vector3 cohesion = GetCohesionVector();
        targetForward = Vector3.RotateTowards(targetForward,
            (targetForward + separation * 2 + alignment + cohesion) / 5, ROTATE_SPEED / 2 * Time.deltaTime, 0f);
        AvoidWalls();
        transform.forward = Vector3.RotateTowards(transform.forward, targetForward, ROTATE_SPEED * Time.deltaTime, 0f);
        transform.Translate(SPEED * Time.deltaTime * Vector3.forward);
    }

    private Vector3 GetSeparationVector()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid boid in BoidsManager.Instance.boids)
        {
            if (boid == this || Vector3.Distance(transform.position, boid.transform.position) > VISION_RADIUS / 2)
            {
                continue;
            }
            sum += boid.transform.position;
            count++;
        }
        if (count == 0)
        {
            return Vector3.zero;
        }
        Vector3 position = sum / count;
        return (-(position - transform.position)).normalized;
    }

    private Vector3 GetAlignmentVector()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid boid in BoidsManager.Instance.boids)
        {
            if (boid == this || Vector3.Distance(transform.position, boid.transform.position) > VISION_RADIUS)
            {
                continue;
            }
            sum += boid.targetForward;
            count++;
        }
        if (count == 0)
        {
            return Vector3.zero;
        }
        return (sum / count).normalized;
    }

    private Vector3 GetCohesionVector()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid boid in BoidsManager.Instance.boids)
        {
            if (boid == this || Vector3.Distance(transform.position, boid.transform.position) > VISION_RADIUS)
            {
                continue;
            }
            sum += boid.transform.position;
            count++;
        }
        if (count == 0)
        {
            return Vector3.zero;
        }
        Vector3 position = sum / count;
        return (position - transform.position).normalized;
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
