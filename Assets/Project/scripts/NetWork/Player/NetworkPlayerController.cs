using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
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

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Camera playerCamera;

    // Components
    private CharacterController controller;

    // === СИНХРОНИЗИРУЕМЫЕ ПЕРЕМЕННЫЕ ===
    // Эти переменные автоматически передаются всем клиентам
    [SyncVar] private bool syncIsSprinting;
    [SyncVar] private bool syncIsMoving;
    [SyncVar] private bool syncIsGrounded = true;

    // === ЛОКАЛЬНЫЕ ПЕРЕМЕННЫЕ ===
    // Это только у самого игрока
    private float verticalVelocity;
    private float cameraPitch;
    private float currentStamina;
    private float staminaRegenTimer;

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
    private bool wasGrounded;

    // === PROPERTIES ===
    public float Stamina => currentStamina;
    public float StaminaNormalized => currentStamina / maxStamina;
    public bool IsSprinting => syncIsSprinting;
    public bool IsMoving => syncIsMoving;
    public bool IsGrounded => syncIsGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null)
            cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder != null && playerCamera == null)
            playerCamera = cameraHolder.GetComponentInChildren<Camera>();

        if (cameraHolder != null)
            defaultCameraY = cameraHolder.localPosition.y;

        currentStamina = maxStamina;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.tag = "MainCamera";

            var listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"Local player spawned: {name}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);

                var listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null)
                    listener.enabled = false;
            }

            Debug.Log($"Remote player spawned: {name}");
        }
    }

    private void Update()
    {
        // Все игроки обновляют визуал (head bob, landing)
        // но управление и логика — только у локального
        if (isLocalPlayer)
        {
            HandleLook();
            HandleMovement();
            HandleJump();
            HandleStamina();

            // Синхронизируем состояние с сервером
            UpdateSyncedState();
        }

        // Визуал работает у всех (для чужих игроков тоже — 
        // они видят своё покачивание камеры от 3 лица... но у них
        // камера выключена, так что это не важно)
        if (isLocalPlayer)
        {
            HandleHeadBob();
            HandleLanding();
            ApplyCameraOffset();
        }
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
            playerCamera.transform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);
        bool isSprintingNow = wantsToSprint
            && currentStamina > 0f
            && moveZ > 0f
            && controller.isGrounded;

        float speed = isSprintingNow ? sprintSpeed : walkSpeed;

        Vector3 moveDir = transform.right * moveX
            + transform.forward * moveZ;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * speed;

        bool isMovingNow = moveDir.sqrMagnitude > 0.01f
            && controller.isGrounded;

        // Гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);

        // Сохраняем локально
        syncIsSprinting = isSprintingNow;
        syncIsMoving = isMovingNow;
    }

    private void HandleJump()
    {
        // Приземление
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
        syncIsGrounded = controller.isGrounded;

        // Прыжок
        if (Input.GetKeyDown(KeyCode.Space)
            && controller.isGrounded
            && currentStamina >= jumpStaminaCost)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity);

            currentStamina -= jumpStaminaCost;
            staminaRegenTimer = staminaRegenDelay;
            bobTimer = 0f;
        }
    }

    private void HandleStamina()
    {
        if (syncIsSprinting)
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

    private void UpdateSyncedState()
    {
        // Здесь можно добавить синхронизацию через Command
        // если нужна более сложная логика
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

        if (syncIsMoving && syncIsSprinting)
        {
            bobSpeed = sprintBobSpeed;
            bobAmount = sprintBobAmount;
        }
        else if (syncIsMoving)
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
        if (cameraHolder == null) return;

        currentBobOffsetY = Mathf.Lerp(
            currentBobOffsetY,
            targetBobOffsetY,
            Time.deltaTime * bobSmoothing);

        float totalOffset = currentBobOffsetY + landingDipOffset;

        Vector3 pos = cameraHolder.localPosition;
        pos.y = defaultCameraY + totalOffset;
        cameraHolder.localPosition = pos;
    }
}
