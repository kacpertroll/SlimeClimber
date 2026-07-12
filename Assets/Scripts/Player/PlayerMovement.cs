using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private Transform groundCheck;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 45f;
    [SerializeField] private float airControlMultiplier = 0.55f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float fallMultiplier = 2.2f;
    [SerializeField] private float jumpCutMultiplier = 0.45f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    private Vector2 moveInput;
    private Vector3 moveDirection;

    private bool isGrounded;
    private bool jumpPressed;
    private bool jumpHeld;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        playerRb.freezeRotation = true;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();

        jumpAction.action.started += OnJumpStarted;
        jumpAction.action.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        jumpAction.action.started -= OnJumpStarted;
        jumpAction.action.canceled -= OnJumpCanceled;

        moveAction.action.Disable();
        jumpAction.action.Disable();
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        HandleTimers();
        CalculateMoveDirection();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        HandleJump();
        ApplyBetterGravity();
    }

    private void ReadInput()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    private void CalculateMoveDirection()
    {
        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection = forward * moveInput.y + right * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();
    }

    private void MovePlayer()
    {
        Vector3 currentHorizontalVelocity = new Vector3(
            playerRb.linearVelocity.x,
            0f,
            playerRb.linearVelocity.z
        );

        Vector3 targetVelocity = moveDirection * moveSpeed;

        float controlMultiplier = isGrounded ? 1f : airControlMultiplier;

        float currentAcceleration = moveInput.sqrMagnitude > 0.01f
            ? acceleration
            : deceleration;

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            currentAcceleration * controlMultiplier * Time.fixedDeltaTime
        );

        playerRb.linearVelocity = new Vector3(
            newHorizontalVelocity.x,
            playerRb.linearVelocity.y,
            newHorizontalVelocity.z
        );
    }

    private void HandleJump()
    {
        bool canJump = coyoteTimer > 0f;
        bool hasBufferedJump = jumpBufferTimer > 0f;

        if (canJump && hasBufferedJump)
        {
            playerRb.linearVelocity = new Vector3(
                playerRb.linearVelocity.x,
                0f,
                playerRb.linearVelocity.z
            );

            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }
    }

    private void ApplyBetterGravity()
    {
        if (playerRb.linearVelocity.y < 0f)
        {
            playerRb.AddForce(
                Vector3.up * Physics.gravity.y * (fallMultiplier - 1f),
                ForceMode.Acceleration
            );
        }

        if (playerRb.linearVelocity.y > 0f && !jumpHeld)
        {
            playerRb.linearVelocity = new Vector3(
                playerRb.linearVelocity.x,
                playerRb.linearVelocity.y * jumpCutMultiplier,
                playerRb.linearVelocity.z
            );
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleTimers()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpPressed = false;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}