using UnityEngine;

public class PlayerControllerScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private CharacterController controller;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float groundAcceleration = 20f;
    [SerializeField] private float groundDeceleration = 25f;
    [SerializeField, Range(0f, 1f)] private float airControl = 0.25f;

    [Header("Jumping")]
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float groundedForce = -2f;
    [SerializeField] private float fallGravityMultiplier = 1.35f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("Crouching")]
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float crouchCameraOffset = -0.35f;
    [SerializeField] private LayerMask ceilingLayers = ~0;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 20f;
    [SerializeField] private float staminaRecoveryPerSecond = 15f;
    [SerializeField] private float staminaRecoveryDelay = 1f;
    [SerializeField] private float staminaResumeThreshold = 15f;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteTimer;
    private float currentStamina;
    private float lastSprintTime = float.NegativeInfinity;
    private float standingHeight;
    private Vector3 standingControllerCenter;
    private Vector3 standingColliderCenter;
    private Vector3 standingCameraPosition;
    private bool isCrouching;
    private bool sprintExhausted;
    private readonly Collider[] standingCheckResults = new Collider[8];

    public float camXRotation = 0f;
    public float StaminaNormalized => maxStamina > 0f ? currentStamina / maxStamina : 0f;
    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (controller == null) controller = GetComponent<CharacterController>();
        if (inputManager == null) inputManager = GetComponent<InputManager>();
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();

        standingHeight = controller.height;
        standingControllerCenter = controller.center;
        standingColliderCenter = capsuleCollider != null ? capsuleCollider.center : Vector3.zero;
        standingCameraPosition = cam.transform.localPosition;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        UpdateGrounding();
        UpdateCrouch();
        UpdateStaminaAndSprint();
        UpdateHorizontalMovement();
        UpdateJumpAndGravity();

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void UpdateGrounding()
    {
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = groundedForce;
        }
        else
        {
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        }
    }

    private void UpdateCrouch()
    {
        bool wantsToCrouch = inputManager.crouchToggled;
        if (!wantsToCrouch && isCrouching && !CanStand())
            wantsToCrouch = true;

        isCrouching = wantsToCrouch;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        float centerOffset = (height - standingHeight) * 0.5f;

        controller.height = height;
        controller.center = standingControllerCenter + Vector3.up * centerOffset;

        if (capsuleCollider != null)
        {
            capsuleCollider.height = height;
            capsuleCollider.center = standingColliderCenter + Vector3.up * centerOffset;
        }

        Vector3 targetCameraPosition = standingCameraPosition + Vector3.up * (isCrouching ? crouchCameraOffset : 0f);
        cam.transform.localPosition = Vector3.MoveTowards(
            cam.transform.localPosition,
            targetCameraPosition,
            crouchTransitionSpeed * Time.deltaTime);
    }

    private bool CanStand()
    {
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Vector3 standingCenterWorld = transform.TransformPoint(standingControllerCenter);
        Vector3 currentCenterWorld = transform.TransformPoint(controller.center);
        Vector3 currentTop = currentCenterWorld + transform.up * (controller.height * 0.5f);
        Vector3 standingTop = standingCenterWorld + transform.up * (standingHeight * 0.5f);
        Vector3 bottom = currentTop;
        Vector3 top = standingTop - transform.up * radius;

        if (Vector3.Dot(top - bottom, transform.up) < 0f)
            top = bottom;

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            standingCheckResults,
            ceilingLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = standingCheckResults[i];
            if (hit != null && !hit.transform.IsChildOf(transform))
                return false;
        }

        return true;
    }

    private void UpdateStaminaAndSprint()
    {
        if (sprintExhausted && currentStamina >= staminaResumeThreshold)
            sprintExhausted = false;

        bool hasForwardInput = inputManager.moveInput.y > 0.1f;
        bool hasMovementInput = inputManager.moveInput.sqrMagnitude > 0.01f;
        IsSprinting = inputManager.sprintHeld
            && hasForwardInput
            && hasMovementInput
            && controller.isGrounded
            && !isCrouching
            && !sprintExhausted
            && currentStamina > 0f;

        if (IsSprinting)
        {
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainPerSecond * Time.deltaTime);
            lastSprintTime = Time.time;

            if (currentStamina <= 0f)
            {
                sprintExhausted = true;
                IsSprinting = false;
            }
        }
        else if (Time.time >= lastSprintTime + staminaRecoveryDelay)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoveryPerSecond * Time.deltaTime);
        }
    }

    private void UpdateHorizontalMovement()
    {
        Vector2 moveInput = Vector2.ClampMagnitude(inputManager.moveInput, 1f);
        Vector3 desiredDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
        float targetSpeed = isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = desiredDirection * targetSpeed;

        float acceleration;
        if (!controller.isGrounded)
            acceleration = groundAcceleration * airControl;
        else if (targetVelocity.sqrMagnitude > horizontalVelocity.sqrMagnitude)
            acceleration = groundAcceleration;
        else
            acceleration = groundDeceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
    }

    private void UpdateJumpAndGravity()
    {
        if (coyoteTimer > 0f && !isCrouching && inputManager.ConsumeJump())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
        }

        float gravityMultiplier = verticalVelocity < 0f ? fallGravityMultiplier : 1f;
        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
    }

    private void LateUpdate()
    {
        camXRotation -= (inputManager.mouseLookInput.y * Time.deltaTime) * inputManager.mouseYSensitivity;
        camXRotation = Mathf.Clamp(camXRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(camXRotation, 0, 0);

        transform.Rotate(Vector3.up * (inputManager.mouseLookInput.x * Time.deltaTime) * inputManager.mouseXSensitivity);
    }

}
