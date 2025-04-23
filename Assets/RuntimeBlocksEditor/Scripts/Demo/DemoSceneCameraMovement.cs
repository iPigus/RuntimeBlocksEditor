using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeBlocksEditor.Demo
{
public class DemoSceneCameraMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private bool invertY = false;

    private Vector3 currentVelocity;
    private Vector2 currentRotation;

    // Start is called before the first frame update
    void Start()
    {
        // Store initial rotation
        currentRotation.y = transform.eulerAngles.x;
        currentRotation.x = transform.eulerAngles.y;
        
        // Show cursor initially
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)|| Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            return;
        }
        // Movement direction vector
        Vector3 direction = Vector3.zero;

        // Forward/backward movement (W/S)
        if (Input.GetKey(KeyCode.W))
            direction += transform.forward;
        if (Input.GetKey(KeyCode.S))
            direction -= transform.forward;

        // Left/right movement (A/D)
        if (Input.GetKey(KeyCode.A))
            direction -= transform.right;
        if (Input.GetKey(KeyCode.D))
            direction += transform.right;

        // Normalize vector to prevent faster diagonal movement
        if (direction.magnitude > 0.1f)
            direction.Normalize();
        float currentSpeed = moveSpeed;


        // Smooth camera movement
        Vector3 targetPosition = transform.position + direction * currentSpeed * Time.deltaTime;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    private void HandleRotation()
    {
        // Rotate camera only when right mouse button is pressed
        if (Input.GetMouseButton(1))
        {
            // Hide and lock cursor during rotation
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * (invertY ? 1 : -1);

            // Update current rotation
            currentRotation.x += mouseX;
            currentRotation.y = Mathf.Clamp(currentRotation.y + mouseY, -89f, 89f); // Clamp vertical rotation

            // Apply rotation
            transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
        }
        else
        {
                // Show cursor when not rotating
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}