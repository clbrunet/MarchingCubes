using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField]
    private float speed = 12f;
    [SerializeField]
    private float sensitivity = 1f;
    [SerializeField]
    private float pitchMin = -89f;
    [SerializeField]
    private float pitchMax = 89f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance.OnPauseStateChanged += GameManager_OnPauseStateChanged;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnPauseStateChanged -= GameManager_OnPauseStateChanged;
    }

    private void GameManager_OnPauseStateChanged(bool isGamePaused)
    {
        Cursor.lockState = isGamePaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.IsGamePaused())
        {
            return;
        }
        Vector3 inputDirection = GetInputDirection();
        float speedMultiplier = GetSpeedMultiplier();
        transform.Translate(speedMultiplier * speed * Time.deltaTime * inputDirection);
        Vector2 mouseInput = GetMouseInput();
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.y += sensitivity * mouseInput.x;
        if (eulerAngles.x > 180f)
        {
            eulerAngles.x = Mathf.Clamp(eulerAngles.x - 360f - sensitivity * mouseInput.y, pitchMin, pitchMax);
        }
        else
        {
            eulerAngles.x = Mathf.Clamp(eulerAngles.x - sensitivity * mouseInput.y, pitchMin, pitchMax);
        }
        transform.eulerAngles = eulerAngles;
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

    private Vector2 GetMouseInput()
    {
        Vector2 input = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        return input;
    }
}
