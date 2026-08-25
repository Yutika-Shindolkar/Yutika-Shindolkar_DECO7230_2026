using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float lookSensitivity = 0.1f;

    InputAction moveAction;
    InputAction lookAction;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // Look around only while right mouse button is held
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 lookDelta = lookAction.ReadValue<Vector2>();
            yaw += lookDelta.x * lookSensitivity;
            pitch -= lookDelta.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Movement now follows wherever you're currently facing
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 movement = (transform.forward * input.y) + (transform.right * input.x);
        transform.position += movement.normalized * moveSpeed * Time.deltaTime;
    }
}