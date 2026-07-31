using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class NetworkSeekerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Camera seekerCamera;
    [SerializeField] private Animator animator;

    // === Синхронизируемые ===
    [SyncVar] private float syncMoveSpeed;
    [SyncVar] private bool syncIsRunning;

    // === Локальные ===
    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    // Оптимизация Command
    private float lastSentSpeed;
    private bool lastSentRunning;
    private const float speedThreshold = 0.1f;

    public bool IsRunning => syncIsRunning;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null)
            cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder != null && seekerCamera == null)
            seekerCamera = cameraHolder.GetComponentInChildren<Camera>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (seekerCamera != null)
        {
            seekerCamera.gameObject.SetActive(true);
            seekerCamera.tag = "MainCamera";

            var listener = seekerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            if (seekerCamera != null)
            {
                seekerCamera.gameObject.SetActive(false);

                var listener = seekerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleLook();
            HandleMovement();
            HandleJump();
        }

        UpdateAnimator();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch, -maxLookAngle, maxLookAngle);

        if (seekerCamera != null)
        {
            seekerCamera.transform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift)
            && moveZ > 0f
            && controller.isGrounded;

        float speed = wantsToRun ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * moveX
            + transform.forward * moveZ;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * speed;

        float currentSpeed = new Vector3(
            moveDir.x, 0f, moveDir.z).magnitude;

        // Отправляем только если изменилось значимо
        if (Mathf.Abs(currentSpeed - lastSentSpeed) > speedThreshold
            || wantsToRun != lastSentRunning)
        {
            CmdUpdateAnimState(currentSpeed, wantsToRun);
            lastSentSpeed = currentSpeed;
            lastSentRunning = wantsToRun;
        }

        // Гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    [Command]
    private void CmdUpdateAnimState(float speed, bool isRunning)
    {
        syncMoveSpeed = speed;
        syncIsRunning = isRunning;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", syncMoveSpeed);
    }
}