using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SeekerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private LayerMask attackMask = ~0;

    [Header("Fog Teleport")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float fogStartDistance = 30f;
    [SerializeField] private float teleportLineDistance = 10f;
    [SerializeField] private float teleportRandomOffset = 15f;

    [Header("Fog Visuals")]
    [SerializeField]
    private Color fogColor =
        new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField] private float maxFogDensity = 0.12f;
    [SerializeField] private float fogTransitionSpeed = 4f;

    // Terrain bounds
    private float terrainMinX, terrainMaxX;
    private float terrainMinZ, terrainMaxZ;

    // Zones
    private float safeMinX, safeMaxX;
    private float safeMinZ, safeMaxZ;
    private float teleportMinX, teleportMaxX;
    private float teleportMinZ, teleportMaxZ;

    // Fog state
    private float currentFogIntensity;
    private bool inFogZone;

    // Cached original fog
    private bool originalFogEnabled;
    private Color originalFogColor;
    private float originalFogDensity;
    private FogMode originalFogMode;
    private bool hasCachedFog;

    [Header("Head Bob")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobSpeed = 12f;
    [SerializeField] private float runBobAmount = 0.09f;
    [SerializeField] private float idleBobSpeed = 1.5f;
    [SerializeField] private float idleBobAmount = 0.015f;
    [SerializeField] private float bobSmoothing = 10f;

    [Header("Attack Animation")]
    [SerializeField] private float attackForward = 0.4f;
    [SerializeField] private float attackDown = 0.15f;
    [SerializeField] private float attackDuration = 0.25f;
    [SerializeField] private float attackReturnSpeed = 8f;

    [Header("Base Lean (постоянно)")]
    [SerializeField] private float baseCameraDropAmount = 0.25f;
    [SerializeField] private float baseCameraForwardAmount = 0.15f;
    [SerializeField] private float baseCameraPitchAmount = 8f;

    [Header("Run Lean (при беге)")]
    [SerializeField] private float runCameraDropAmount = 0.5f;
    [SerializeField] private float runCameraForwardAmount = 0.35f;
    [SerializeField] private float runCameraPitchAmount = 20f;

    [Header("Lean Transitions")]
    [SerializeField] private float leanSpeed = 4f;
    [SerializeField] private float leanReturnSpeed = 6f;

    // State
    private float currentLeanY;
    private float currentLeanZ;
    private float currentLeanPitch;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Landing")]
    [SerializeField] private float landingDip = 0.2f;
    [SerializeField] private float landingRecovery = 8f;

    // Components
    private CharacterController controller;
    private Transform cameraHolder;
    private Camera seekerCamera;

    // Movement state
    private float verticalVelocity;
    private float cameraPitch;
    private bool isRunning;
    private bool isMoving;
    private float attackTimer;
    private bool wasGrounded;

    // Camera offsets
    private Vector3 cameraBaseLocalPos;
    private float bobTimer;
    private float currentBobY;
    private float currentBobX;
    private float targetBobY;
    private float targetBobX;

    // Attack offset
    private Vector3 attackOffset;
    private Vector3 attackVelocity;

    // Landing
    private float landingOffset;
    private float landingVelocity;

    public bool IsRunning => isRunning;
    public bool CanAttack => attackTimer <= 0f;

    private void Awake()
    {
        
        controller = GetComponent<CharacterController>();

        cameraHolder = transform.Find("CameraHolder");

        if (cameraHolder != null)
        {
            seekerCamera =
                cameraHolder.GetComponentInChildren<Camera>();
            cameraBaseLocalPos = cameraHolder.localPosition;
        }
        else
        {
            seekerCamera = GetComponentInChildren<Camera>();
        }
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Твоё существующее припадание
        currentLeanY = -baseCameraDropAmount;
        currentLeanZ = baseCameraForwardAmount;
        currentLeanPitch = baseCameraPitchAmount;

        // === НОВОЕ ===
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        CalculateBounds();
        CacheFogSettings();
    }

    private void CalculateBounds()
    {
        if (terrain == null) return;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        terrainMinX = tPos.x;
        terrainMaxX = tPos.x + tSize.x;
        terrainMinZ = tPos.z;
        terrainMaxZ = tPos.z + tSize.z;

        safeMinX = terrainMinX + fogStartDistance;
        safeMaxX = terrainMaxX - fogStartDistance;
        safeMinZ = terrainMinZ + fogStartDistance;
        safeMaxZ = terrainMaxZ - fogStartDistance;

        teleportMinX = terrainMinX + teleportLineDistance;
        teleportMaxX = terrainMaxX - teleportLineDistance;
        teleportMinZ = terrainMinZ + teleportLineDistance;
        teleportMaxZ = terrainMaxZ - teleportLineDistance;
    }

    private void CacheFogSettings()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogMode = RenderSettings.fogMode;
        hasCachedFog = true;
    }

    private void Update()
    {
        if (DebugConsole.IsOpen) return;

        HandleLook();
        HandleMovement();
        HandleJump();
        HandleAttack();
        UpdateCooldown();
        HandleHeadBob();
        HandleLanding();
        UpdateAttackOffset();
        UpdateLean();
        ApplyCameraOffset();

        // === НОВОЕ ===
        UpdateFogZone();
        UpdateFogVisuals();
        CheckFogTeleport();

        UpdateAnimator();
    }
    private void OnEnable()
    {
        if (hasCachedFog)
            CacheFogSettings();
    }

    private void OnDisable()
    {
        if (hasCachedFog)
        {
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogMode = originalFogMode;
        }

        currentFogIntensity = 0f;
        inFogZone = false;
    }
    private void UpdateFogZone()
    {
        Vector3 pos = transform.position;

        bool inFog =
            pos.x < safeMinX || pos.x > safeMaxX ||
            pos.z < safeMinZ || pos.z > safeMaxZ;

        inFogZone = inFog;

        float targetIntensity = 0f;

        if (inFog)
        {
            float fogDepth = GetFogDepth(pos);
            float maxDepth = fogStartDistance
                - teleportLineDistance;
            targetIntensity = Mathf.Clamp01(
                fogDepth / maxDepth);
        }

        currentFogIntensity = Mathf.Lerp(
            currentFogIntensity,
            targetIntensity,
            Time.deltaTime * fogTransitionSpeed);
    }

    private float GetFogDepth(Vector3 pos)
    {
        float depthLeft = safeMinX - pos.x;
        float depthRight = pos.x - safeMaxX;
        float depthBottom = safeMinZ - pos.z;
        float depthTop = pos.z - safeMaxZ;

        return Mathf.Max(0f,
            depthLeft, depthRight,
            depthBottom, depthTop);
    }

    private void UpdateFogVisuals()
    {
        if (currentFogIntensity > 0.01f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            RenderSettings.fogColor = Color.Lerp(
                originalFogColor,
                fogColor,
                currentFogIntensity);

            float targetDensity = Mathf.Lerp(
                originalFogDensity,
                maxFogDensity,
                currentFogIntensity);

            RenderSettings.fogDensity = Mathf.Max(
                originalFogDensity,
                targetDensity);
        }
        else
        {
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogMode = originalFogMode;
        }
    }

    private void CheckFogTeleport()
    {
        Vector3 pos = transform.position;

        bool needsTeleport =
            pos.x < teleportMinX || pos.x > teleportMaxX ||
            pos.z < teleportMinZ || pos.z > teleportMaxZ;

        if (needsTeleport)
            TeleportThroughFog();
    }

    private void TeleportThroughFog()
    {
        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos;

        float depthLeft = teleportMinX - currentPos.x;
        float depthRight = currentPos.x - teleportMaxX;
        float depthBottom = teleportMinZ - currentPos.z;
        float depthTop = currentPos.z - teleportMaxZ;

        float maxDepth = Mathf.Max(
            depthLeft, depthRight,
            depthBottom, depthTop);

        float spawnOffset = 1f;

        if (maxDepth == depthLeft)
        {
            newPos.x = teleportMaxX - spawnOffset;
            newPos.z = currentPos.z
                + Random.Range(-teleportRandomOffset,
                    teleportRandomOffset);
        }
        else if (maxDepth == depthRight)
        {
            newPos.x = teleportMinX + spawnOffset;
            newPos.z = currentPos.z
                + Random.Range(-teleportRandomOffset,
                    teleportRandomOffset);
        }
        else if (maxDepth == depthBottom)
        {
            newPos.z = teleportMaxZ - spawnOffset;
            newPos.x = currentPos.x
                + Random.Range(-teleportRandomOffset,
                    teleportRandomOffset);
        }
        else if (maxDepth == depthTop)
        {
            newPos.z = teleportMinZ + spawnOffset;
            newPos.x = currentPos.x
                + Random.Range(-teleportRandomOffset,
                    teleportRandomOffset);
        }

        newPos.x = Mathf.Clamp(newPos.x,
            terrainMinX + 2f, terrainMaxX - 2f);
        newPos.z = Mathf.Clamp(newPos.z,
            terrainMinZ + 2f, terrainMaxZ - 2f);

        // Правильная высота через terrain
        newPos.y = terrain.SampleHeight(newPos)
            + terrain.transform.position.y;

        // Телепорт с отключением CC
        controller.enabled = false;
        transform.position = newPos;
        controller.enabled = true;

        // Сброс скорости
        verticalVelocity = -2f;

        Debug.Log($"Seeker teleported: {currentPos} → {newPos}");
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 horizontalVel = new Vector3(
            controller.velocity.x, 0f,
            controller.velocity.z);
        float speed = horizontalVel.magnitude;

        animator.SetFloat("Speed", speed);

        // Debug — временно добавь
        Debug.Log($"Animator Speed: {speed}");
    }
    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X")
            * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y")
            * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch, -maxLookAngle, maxLookAngle);

        if (seekerCamera != null)
        {
            // Добавляем pitch от бега
            float totalPitch = cameraPitch + currentLeanPitch;
            seekerCamera.transform.localRotation =
                Quaternion.Euler(totalPitch, 0f, 0f);
        }
    }
    private void UpdateLean()
    {
        float targetY;
        float targetZ;
        float targetPitch;

        if (isRunning)
        {
            // Бег — максимальное припадание
            targetY = -runCameraDropAmount;
            targetZ = runCameraForwardAmount;
            targetPitch = runCameraPitchAmount;
        }
        else
        {
            // Обычная поза — постоянное лёгкое припадание
            targetY = -baseCameraDropAmount;
            targetZ = baseCameraForwardAmount;
            targetPitch = baseCameraPitchAmount;
        }

        // Скорость перехода
        float speed = isRunning ? leanSpeed : leanReturnSpeed;

        currentLeanY = Mathf.Lerp(
            currentLeanY, targetY,
            Time.deltaTime * speed);

        currentLeanZ = Mathf.Lerp(
            currentLeanZ, targetZ,
            Time.deltaTime * speed);

        currentLeanPitch = Mathf.Lerp(
            currentLeanPitch, targetPitch,
            Time.deltaTime * speed);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        isRunning = Input.GetKey(KeyCode.LeftShift)
            && moveZ > 0f
            && controller.isGrounded;

        float speed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDir = transform.right * moveX
            + transform.forward * moveZ;
        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * speed;

        isMoving = moveDir.sqrMagnitude > 0.01f
            && controller.isGrounded;

        // Landing detection
        if (controller.isGrounded && !wasGrounded)
        {
            landingOffset = -landingDip;
        }
        wasGrounded = controller.isGrounded;

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space)
            && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity);
            bobTimer = 0f;
        }
    }

    // Добавь ссылку для отложенного попадания
    private RaycastHit? pendingAttackHit;

    private void HandleAttack()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!CanAttack) return;
        if (seekerCamera == null) return;

        if (animator != null)
            animator.SetTrigger("Attack");

        Ray ray = seekerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        bool hitSomething = false;

        if (Physics.Raycast(
            ray, out RaycastHit hit,
            attackRange, attackMask))
        {
            var player = hit.collider
                .GetComponentInParent<PlayerHealth>();

            if (player != null)
            {
                pendingAttackHit = hit;
                hitSomething = true;
            }
        }

        if (hitSomething)
            attackTimer = attackCooldown;
        else
        {
            pendingAttackHit = null;
            attackTimer = attackCooldown * 0.5f;
        }

        Debug.Log($"HandleAttack: starting coroutine, hit={hitSomething}");
        StartCoroutine(AttackMotion(hitSomething));
    }

    /// <summary>
    /// Вызывается из Animation Event в момент касания
    /// </summary>
    public void OnAttackHit()
    {
        if (pendingAttackHit == null) return;

        var hit = pendingAttackHit.Value;

        // Проверяем что цель ещё там
        if (hit.collider == null)
        {
            pendingAttackHit = null;
            return;
        }

        var player = hit.collider
            .GetComponentInParent<PlayerHealth>();

        if (player != null)
        {
            // Проверка дистанции
            float dist = Vector3.Distance(
                transform.position,
                player.transform.position);

            if (dist <= attackRange + 0.5f)
            {
                player.TakeDamage();
                Debug.Log($"Удар в момент анимации!");
            }
        }

        pendingAttackHit = null;
    }

    private System.Collections.IEnumerator AttackMotion(bool hit)
    {
        Debug.Log($"AttackMotion started, hit={hit}");

        float multiplier = hit ? 1f : 0.6f;
        float elapsed = 0f;

        while (elapsed < attackDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (attackDuration * 0.4f);
            t = t * t;

            attackOffset = new Vector3(
                0f,
                -attackDown * multiplier * t,
                attackForward * multiplier * t
            );

            Debug.Log($"AttackMotion tick: offset={attackOffset}");

            yield return null;
        }

        Debug.Log("AttackMotion finished");
    }

    private void UpdateAttackOffset()
    {
        attackOffset = Vector3.SmoothDamp(
            attackOffset,
            Vector3.zero,
            ref attackVelocity,
            1f / attackReturnSpeed
        );
    }


    private void UpdateCooldown()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    private void HandleHeadBob()
    {
        if (!controller.isGrounded)
        {
            // В воздухе — нет покачивания
            targetBobY = 0f;
            targetBobX = 0f;
            return;
        }

        float bobSpeed;
        float bobAmount;

        if (isMoving && isRunning)
        {
            bobSpeed = runBobSpeed;
            bobAmount = runBobAmount;
        }
        else if (isMoving)
        {
            bobSpeed = walkBobSpeed;
            bobAmount = walkBobAmount;
        }
        else
        {
            // Idle — медленное дыхание
            bobSpeed = idleBobSpeed;
            bobAmount = idleBobAmount;
        }

        bobTimer += Time.deltaTime * bobSpeed;

        // Y — вверх-вниз (шаги)
        targetBobY = Mathf.Sin(bobTimer) * bobAmount;

        // X — влево-вправо (перекат)
        targetBobX = Mathf.Cos(bobTimer * 0.5f)
            * bobAmount * 0.4f;
    }

    private void HandleLanding()
    {
        // Плавный возврат из landing dip
        if (landingOffset < 0f)
        {
            landingOffset = Mathf.SmoothDamp(
                landingOffset,
                0f,
                ref landingVelocity,
                1f / landingRecovery
            );

            if (Mathf.Abs(landingOffset) < 0.001f)
                landingOffset = 0f;
        }
    }

    private void ApplyCameraOffset()
    {
        if (cameraHolder == null) return;

        currentBobY = Mathf.Lerp(
            currentBobY, targetBobY,
            Time.deltaTime * bobSmoothing);

        currentBobX = Mathf.Lerp(
            currentBobX, targetBobX,
            Time.deltaTime * bobSmoothing);

        Vector3 offset = new Vector3(
            currentBobX + attackOffset.x,
            currentBobY + landingOffset + attackOffset.y + currentLeanY,
            attackOffset.z + currentLeanZ
        );

        cameraHolder.localPosition =
            cameraBaseLocalPos + offset;
    }

    public float GetAttackCooldownNormalized()
    {
        if (attackTimer <= 0f) return 0f;
        return attackTimer / attackCooldown;
    }
}