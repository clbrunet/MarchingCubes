using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private const float RAYS_OFFSET_ANGLE = 30f;
    private const float RAYCAST_DISTANCE = 2.5f;
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
        if (Mathf.Abs(transform.position.x) > 5f || Mathf.Abs(transform.position.y) > 2.5f
            || Mathf.Abs(transform.position.z) > 5f)
        {
            Debug.LogError(name + " en dehors : " + transform.position);
        }
        if (Physics.Raycast(transform.position, targetForward, RAYCAST_DISTANCE, raycastLayer))
        {
            List<Vector3> possibleDirections = new();
            foreach (Vector3 rayDirection in raysDirections)
            {
                Vector3 direction = transform.rotation * rayDirection;
                if (!Physics.Raycast(transform.position, direction, RAYCAST_DISTANCE, raycastLayer))
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
        transform.forward = Vector3.RotateTowards(transform.forward, targetForward, ROTATE_SPEED * Time.deltaTime, 0f);
        transform.Translate(SPEED * Time.deltaTime * Vector3.forward);
    }

    private void OnDrawGizmos()
    {
        foreach (Vector3 rayDirection in raysDirections)
        {
            Vector3 direction = transform.rotation * rayDirection;
            Gizmos.color = Color.green;
            if (Physics.Raycast(transform.position, direction, RAYCAST_DISTANCE, raycastLayer))
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawLine(transform.position, transform.position + RAYCAST_DISTANCE * direction);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + RAYCAST_DISTANCE / 2 * targetForward);
    }
}
