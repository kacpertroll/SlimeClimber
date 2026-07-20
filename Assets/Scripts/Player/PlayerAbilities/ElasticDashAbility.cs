using UnityEngine;
using UnityEngine.InputSystem;

// Prototype: slime compresses and launches in the current move-input direction (or current
// facing direction if no input is held). Takes exclusive control of the rigidbody for the
// dash's duration via PlayerAbilityController, then hands control back to PlayerMovement.
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAbilityController))]
public class ElasticDashAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAbilityController abilityController;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerVisual;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private bool lockVerticalDuringDash = true;

    // Hook points for juice: VFX burst, camera FOV kick, squash/stretch, SFX, etc.
    public event System.Action OnDashStarted;
    public event System.Action OnDashEnded;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private Vector3 dashDirection;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();
    }

    private void OnEnable()
    {
        dashAction.action.Enable();
        dashAction.action.performed += OnDashInput;
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashInput;
        dashAction.action.Disable();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            return;

        Rigidbody rb = playerMovement.Rb;

        rb.linearVelocity = lockVerticalDuringDash
            ? dashDirection * dashSpeed
            : new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
            EndDash();
    }

    private void OnDashInput(InputAction.CallbackContext context)
    {
        TryStartDash();
    }

    private void TryStartDash()
    {
        if (isDashing || cooldownTimer > 0f)
            return;

        if (!abilityController.TryEnterState(AbilityState.Dashing))
            return; // something else already has movement control

        dashDirection = GetDashDirection();
        isDashing = true;
        dashTimer = dashDuration;

        OnDashStarted?.Invoke();
    }

    private Vector3 GetDashDirection()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f && orientation != null)
        {
            Vector3 forward = orientation.forward;
            Vector3 right = orientation.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 dir = forward * moveInput.y + right * moveInput.x;

            if (dir.sqrMagnitude > 0.01f)
                return dir.normalized;
        }

        // Fallback: jeœli gracz nie wciska inputu, dashuj tam, gdzie patrzy model postaci
        if (playerVisual != null)
        {
            Vector3 facing = playerVisual.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude > 0.01f)
                return facing.normalized;
        }

        Vector3 defaultForward = transform.forward;
        defaultForward.y = 0f;
        return defaultForward.normalized;
    }

    private void EndDash()
    {
        isDashing = false;
        cooldownTimer = dashCooldown;

        abilityController.ExitState(AbilityState.Dashing);

        OnDashEnded?.Invoke();
    }
}