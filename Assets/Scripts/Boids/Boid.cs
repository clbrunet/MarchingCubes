using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private const float RAYS_OFFSET_ANGLE = 45f;
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
    public Vector3 targetForward;

    private void Awake()
    {
        targetForward = transform.forward;
    }

    public void UpdateBoid(Vector3 boidAITargetForward)
    {
        targetForward = Vector3.RotateTowards(targetForward, boidAITargetForward, ROTATE_SPEED / 4f * Time.deltaTime, 0f);
        AvoidWalls();
        transform.forward = Vector3.RotateTowards(transform.forward, targetForward, ROTATE_SPEED * Time.deltaTime, 0f);
        transform.Translate(SPEED * Time.deltaTime * Vector3.forward);
    }

    private void AvoidWalls()
    {
        if (!Physics.Raycast(transform.position, targetForward, WALL_RAYCAST_DISTANCE, raycastLayer))
        {
            return;
        }
        int index = Random.Range(0, raysDirections.Length);
        for (int i = 0; i < raysDirections.Length; i++)
        {
            Vector3 direction = transform.rotation * raysDirections[index];
            if (!Physics.Raycast(transform.position, direction, WALL_RAYCAST_DISTANCE, raycastLayer))
            {
                targetForward = direction;
            }
            index++;
            if (index >= raysDirections.Length)
            {
                index = 0;
            }
        }
        targetForward = -transform.forward;
    }
}
