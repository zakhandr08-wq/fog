using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    [SerializeField] private float jumpStaminaCost = 5f;

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

    [Header("Jump Audio")]
    [SerializeField] private AudioSource oneShotAudio;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float jumpVolume = 0.5f;

    // Components
    private CharacterController controller;
    private Transform cameraHolder;
    private Camera playerCamera;
    private PlayerHealth health;

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
        health = GetComponent<PlayerHealth>();

        cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder == null)
        {
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
        if (DebugConsole.IsOpen) return;

        HandleLook();
        HandleStamina();
        HandleLanding();
        ApplyCameraOffset();

        bool canMove = health == null
            || (!health.IsDowned && !health.IsDead);

        if (canMove)
        {
            HandleMovement();
            HandleJump();
            HandleHeadBob();
        }
        else
        {
            HandleGravityOnly();
        }
    }

    private void HandleGravityOnly()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = Vector3.zero;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        isSprinting = false;
        isMoving = false;
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

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);
        isSprinting = wantsToSprint
            && currentStamina > 0f
            && moveZ > 0f
            && controller.isGrounded;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 moveDir = transform.right * moveX
            + transform.forward * moveZ;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * speed;

        isMoving = moveDir.sqrMagnitude > 0.01f
            && controller.isGrounded;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (!controller.isGrounded && verticalVelocity < 0f)
        {
            if (!isFalling)
            {
                isFalling = true;
                fallStartY = transform.position.y;
            }
        }

        if (controller.isGrounded && !wasGrounded)
        {
            if (isFalling)
            {
                float fallDistance = fallStartY - transform.position.y;

                if (fallDistance > 0.5f)
                {
                    float dipAmount = Mathf.Clamp(
                        fallDistance * 0.05f, 0.02f, landingDip);
                    landingDipOffset = -dipAmount;
                }

                isFalling = false;
            }
        }

        wasGrounded = controller.isGrounded;

        if (Input.GetKeyDown(KeyCode.Space)
            && controller.isGrounded
            && currentStamina >= jumpStaminaCost)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity);

            currentStamina -= jumpStaminaCost;
            staminaRegenTimer = staminaRegenDelay;
            bobTimer = 0f;

            PlayJumpSound();
        }
    }

    private void PlayJumpSound()
    {
        if (jumpSound == null || oneShotAudio == null) return;

        oneShotAudio.pitch = Random.Range(0.9f, 1.1f);
        oneShotAudio.PlayOneShot(jumpSound, jumpVolume);
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
            bobSpeed = idleBobSpeed;
            bobAmount = idleBobAmount;
        }

        bobTimer += Time.deltaTime * bobSpeed;
        targetBobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
    }

    private void HandleLanding()
    {
        if (landingDipOffset < 0f)
        {
            landingDipOffset = Mathf.SmoothDamp(
                landingDipOffset,
                0f,
                ref landingDipVelocity,
                1f / landingDipSpeed);

            if (Mathf.Abs(landingDipOffset) < 0.001f)
                landingDipOffset = 0f;
        }
    }

    private void ApplyCameraOffset()
    {
        currentBobOffsetY = Mathf.Lerp(
            currentBobOffsetY,
            targetBobOffsetY,
            Time.deltaTime * bobSmoothing);

        float totalOffset = currentBobOffsetY + landingDipOffset;

        Vector3 pos = cameraHolder.localPosition;
        pos.y = defaultCameraY + totalOffset;
        cameraHolder.localPosition = pos;
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }
}