using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrain = 10f;
    [SerializeField] private float staminaRegen = 8f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField] private float jumpStaminaCost = 15f;

    [Header("Head Bob")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmount = 0.03f;
    [SerializeField] private float sprintBobSpeed = 14f;
    [SerializeField] private float sprintBobAmount = 0.05f;
    [SerializeField] private float idleBobSpeed = 2f;
    [SerializeField] private float idleBobAmount = 0.005f;
    [SerializeField] private float bobSmoothing = 12f;

    [Header("Landing")]
    [SerializeField] private float landingDip = 0.15f;
    [SerializeField] private float landingDipSpeed = 10f;

    // Components
    private CharacterController controller;
    private Transform cameraHolder;
    private Camera playerCamera;

    // Movement state
    private float verticalVelocity;
    private float cameraPitch;
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isSprinting;
    private bool isMoving;
    private bool wasGrounded;

    // Head bob state
    private float bobTimer;
    private float currentBobOffsetY;
    private float targetBobOffsetY;
    private float defaultCameraY;

    // Landing state
    private float landingDipOffset;
    private float landingDipVelocity;
    private float fallStartY;
    private bool isFalling;

    // Properties
    public float Stamina => currentStamina;
    public float StaminaNormalized => currentStamina / maxStamina;
    public bool IsSprinting => isSprinting;
    public bool IsGrounded => controller.isGrounded;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder == null)
        {
            // Fallback: find camera directly
            playerCamera = GetComponentInChildren<Camera>();
            cameraHolder = playerCamera.transform.parent;
        }
        else
        {
            playerCamera = cameraHolder.GetComponentInChildren<Camera>();
        }

        currentStamina = maxStamina;
        defaultCameraY = cameraHolder.localPosition.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleStamina();
        HandleHeadBob();
        HandleLanding();
        ApplyCameraOffset();
    }
    /// <summary>
    /// Сбрасывает вертикальную скорость.
    /// Вызывается при телепорте чтобы не подбрасывало.
    /// </summary>
    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Sprint check
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);
        isSprinting = wantsToSprint
            && currentStamina > 0f
            && moveZ > 0f
            && controller.isGrounded;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        // Direction
        Vector3 moveDir = transform.right * moveX
            + transform.forward * moveZ;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * speed;

        // Track if moving
        isMoving = moveDir.sqrMagnitude > 0.01f
            && controller.isGrounded;

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }

    private void HandleJump()
    {
        // Track falling
        if (!controller.isGrounded && verticalVelocity < 0f)
        {
            if (!isFalling)
            {
                isFalling = true;
                fallStartY = transform.position.y;
            }
        }

        // Landing detection
        if (controller.isGrounded && !wasGrounded)
        {
            if (isFalling)
            {
                float fallDistance = fallStartY - transform.position.y;

                if (fallDistance > 0.5f)
                {
                    // Scale dip by fall distance
                    float dipAmount = Mathf.Clamp(
                        fallDistance * 0.05f, 0.02f, landingDip);
                    landingDipOffset = -dipAmount;
                }

                isFalling = false;
            }
        }

        wasGrounded = controller.isGrounded;

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space)
            && controller.isGrounded
            && currentStamina >= jumpStaminaCost)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity);

            currentStamina -= jumpStaminaCost;
            staminaRegenTimer = staminaRegenDelay;

            // Reset bob
            bobTimer = 0f;
        }
    }

    private void HandleStamina()
    {
        if (isSprinting)
        {
            currentStamina -= staminaDrain * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            staminaRegenTimer -= Time.deltaTime;

            if (staminaRegenTimer <= 0f)
            {
                currentStamina += staminaRegen * Time.deltaTime;
                currentStamina = Mathf.Min(
                    maxStamina, currentStamina);
            }
        }
    }

    private void HandleHeadBob()
    {
        if (!controller.isGrounded)
        {
            // No bob in air
            targetBobOffsetY = 0f;
            return;
        }

        float bobSpeed;
        float bobAmount;

        if (isMoving && isSprinting)
        {
            bobSpeed = sprintBobSpeed;
            bobAmount = sprintBobAmount;
        }
        else if (isMoving)
        {
            bobSpeed = walkBobSpeed;
            bobAmount = walkBobAmount;
        }
        else
        {
            // Idle breathing
            bobSpeed = idleBobSpeed;
            bobAmount = idleBobAmount;
        }

        bobTimer += Time.deltaTime * bobSpeed;
        targetBobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
    }

    private void HandleLanding()
    {
        // Recover from landing dip
        if (landingDipOffset < 0f)
        {
            landingDipOffset = Mathf.SmoothDamp(
                landingDipOffset,
                0f,
                ref landingDipVelocity,
                1f / landingDipSpeed
            );

            // Snap to zero when close enough
            if (Mathf.Abs(landingDipOffset) < 0.001f)
            {
                landingDipOffset = 0f;
            }
        }
    }

    private void ApplyCameraOffset()
    {
        // Smooth bob
        currentBobOffsetY = Mathf.Lerp(
            currentBobOffsetY,
            targetBobOffsetY,
            Time.deltaTime * bobSmoothing
        );

        // Apply to camera holder
        float totalOffset = currentBobOffsetY + landingDipOffset;

        Vector3 pos = cameraHolder.localPosition;
        pos.y = defaultCameraY + totalOffset;
        cameraHolder.localPosition = pos;
    }
}