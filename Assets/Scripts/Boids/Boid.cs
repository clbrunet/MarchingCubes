using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private const float RAYS_OFFSET_ANGLE = 30f;
    private const float VISION_RADIUS = 2.5f;
    private const float SPEED = 1f;
    private const float ROTATE_SPEED = SPEED * 100f * Mathf.Deg2Rad;

    [SerializeField]
    private LayerMask raycastLayer;
    private readonly Vector3[] raysDirections =
    {
        Quaternion.Euler(0f, -RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(0f, RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
        Quaternion.Euler(-RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
        Quaternion.Euler(0f, 2f * -RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(0f, 2f * RAYS_OFFSET_ANGLE, 0f) * Vector3.forward,
        Quaternion.Euler(2f * RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
        Quaternion.Euler(2f * -RAYS_OFFSET_ANGLE, 0f, 0f) * Vector3.forward,
    };
    private Vector3 targetForward;

    private void Start()
    {
        targetForward = transform.forward;
    }

    private void Update()
    {
        AlignInShoal();
        AvoidWalls();
        transform.forward = Vector3.RotateTowards(transform.forward, targetForward, ROTATE_SPEED * Time.deltaTime, 0f);
        transform.Translate(SPEED * Time.deltaTime * Vector3.forward);
    }

    private void AlignInShoal()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Boid boid in BoidsManager.Instance.boids)
        {
            if (Vector3.Distance(transform.position, boid.transform.position) > VISION_RADIUS)
            {
                continue;
            }
            sum += boid.targetForward;
            count++;
        }
        targetForward = (targetForward + sum / count) / 2;
    }

    private void AvoidWalls()
    {
        if (Physics.Raycast(transform.position, targetForward, VISION_RADIUS, raycastLayer))
        {
            List<Vector3> possibleDirections = new();
            foreach (Vector3 rayDirection in raysDirections)
            {
                Vector3 direction = transform.rotation * rayDirection;
                if (!Physics.Raycast(transform.position, direction, VISION_RADIUS, raycastLayer))
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
