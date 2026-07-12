using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Rigidbody playerRb;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 1.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        RotateOrientation();
        RotatePlayerObject();
    }

    private void RotateOrientation()
    {
        Vector3 viewDir = player.position - new Vector3(
            transform.position.x,
            player.position.y,
            transform.position.z
        );

        orientation.forward = viewDir.normalized;
    }

    private void RotatePlayerObject()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        Vector3 inputDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            playerObj.forward = Vector3.Slerp(
                playerObj.forward,
                inputDir.normalized,
                Time.deltaTime * rotationSpeed
            );
        }
    }
    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }
}