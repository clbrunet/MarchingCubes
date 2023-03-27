using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField]
    private float speed = 8;

    private void Update()
    {
        Vector3 inputDirection = GetInputDirection();
        float speedMultiplier = GetSpeedMultiplier();
        transform.Translate(speedMultiplier * speed * Time.deltaTime * inputDirection);
    }

    private Vector3 GetInputDirection()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            input.z += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            input.z -= 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            input.x -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            input.x += 1f;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            input.y -= 1f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            input.y += 1f;
        }
        return input.normalized;
    }

    private float GetSpeedMultiplier()
    {
        float speedMultiplier = 1f;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speedMultiplier *= 2f;
        }
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            speedMultiplier /= 2f;
        }
        return speedMultiplier;
    }
}
