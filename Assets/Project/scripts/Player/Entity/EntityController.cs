using UnityEngine;

public class EntityController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float normalSpeed = 15f;
    [SerializeField] private float fastSpeed = 40f;
    [SerializeField] private float slowSpeed = 5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 6f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    [Header("Bounds")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float minHeight = 2f;
    [SerializeField] private float maxHeight = 60f;

    [Header("Vision")]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float zoomFOV = 30f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Fog Teleport")]
    [SerializeField] private float fogStartDistance = 30f;
    [SerializeField] private float teleportLineDistance = 10f;
    [SerializeField] private float teleportRandomOffset = 15f;

    [Header("Fog Visuals")]
    [SerializeField]
    private Color fogColor =
        new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField] private float maxFogDensity = 0.12f;
    [SerializeField] private float fogTransitionSpeed = 4f;

    // Components
    private Camera entityCamera;

    // Movement
    private Vector3 currentVelocity;
    private float cameraPitch;
    private float cameraYaw;
    private float targetFOV;

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

    // Original fog
    private bool originalFogEnabled;
    private Color originalFogColor;
    private float originalFogDensity;
    private FogMode originalFogMode;
    private bool hasCachedFog;

    private void Start()
    {
        entityCamera = GetComponentInChildren<Camera>();

        if (entityCamera == null)
        {
            Debug.LogError("Entity camera not found!");
            return;
        }

        Vector3 euler = transform.eulerAngles;
        cameraYaw = euler.y;
        cameraPitch = euler.x;

        targetFOV = baseFOV;
        entityCamera.fieldOfView = baseFOV;

        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        CalculateBounds();
        CacheFogSettings();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        // Восстановим кеш при активации
        if (hasCachedFog)
        {
            CacheFogSettings();
        }
    }

    private void OnDisable()
    {
        // Восстановить оригинальный туман
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

    private void CacheFogSettings()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogMode = RenderSettings.fogMode;
        hasCachedFog = true;
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

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleZoom();
        UpdateFogZone();
        UpdateFogVisuals();
        CheckFogTeleport();
        ClampHeight();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraYaw += mouseX;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch, -maxLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(
            cameraPitch, cameraYaw, 0f);
    }

    private void HandleMovement()
    {
        float speed = normalSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed = fastSpeed;
        else if (Input.GetKey(KeyCode.LeftControl))
            speed = slowSpeed;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float moveY = 0f;
        if (Input.GetKey(KeyCode.Space)) moveY += 1f;
        if (Input.GetKey(KeyCode.C)) moveY -= 1f;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 horizontalDir =
            forward * moveZ + right * moveX;
        horizontalDir.y = 0f;

        Vector3 targetVelocity = horizontalDir.normalized
            * speed;
        targetVelocity.y = moveY * speed;

        float rate = targetVelocity.magnitude > 0.01f
            ? acceleration
            : deceleration;

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            Time.deltaTime * rate
        );

        transform.position += currentVelocity * Time.deltaTime;
    }

    private void HandleZoom()
    {
        if (Input.GetMouseButton(1))
            targetFOV = zoomFOV;
        else
            targetFOV = baseFOV;

        entityCamera.fieldOfView = Mathf.Lerp(
            entityCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );
    }

    private void UpdateFogZone()
    {
        Vector3 pos = transform.position;

        // Проверяем, в зоне ли тумана
        bool inFog =
            pos.x < safeMinX || pos.x > safeMaxX ||
            pos.z < safeMinZ || pos.z > safeMaxZ;

        inFogZone = inFog;

        // Рассчитываем интенсивность
        float targetIntensity = 0f;

        if (inFog)
        {
            float fogDepth = GetFogDepth(pos);
            float maxDepth = fogStartDistance
                - teleportLineDistance;
            targetIntensity = Mathf.Clamp01(
                fogDepth / maxDepth);
        }

        // Плавное изменение
        currentFogIntensity = Mathf.Lerp(
            currentFogIntensity,
            targetIntensity,
            Time.deltaTime * fogTransitionSpeed
        );
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
                currentFogIntensity
            );

            float targetDensity = Mathf.Lerp(
                originalFogDensity,
                maxFogDensity,
                currentFogIntensity
            );

            RenderSettings.fogDensity = Mathf.Max(
                originalFogDensity,
                targetDensity
            );
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
        {
            TeleportThroughFog();
        }
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
            depthLeft, depthRight, depthBottom, depthTop);

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

        // Сохранить высоту над землёй
        if (terrain != null)
        {
            float oldTerrainHeight = terrain.SampleHeight(
                currentPos) + terrain.transform.position.y;
            float heightAboveGround = currentPos.y
                - oldTerrainHeight;

            float newTerrainHeight = terrain.SampleHeight(
                newPos) + terrain.transform.position.y;
            newPos.y = newTerrainHeight + heightAboveGround;
        }

        transform.position = newPos;

        // Не сбрасываем скорость — иллюзия непрерывного полёта
        // currentVelocity остаётся тем же
    }

    private void ClampHeight()
    {
        if (terrain == null) return;

        Vector3 pos = transform.position;

        float terrainHeight = terrain.SampleHeight(pos)
            + terrain.transform.position.y;

        float minY = terrainHeight + minHeight;
        float maxY = terrainHeight + maxHeight;

        // Мягко тормозим у границ
        if (pos.y < minY)
        {
            pos.y = minY;
            if (currentVelocity.y < 0f)
                currentVelocity.y = 0f;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            if (currentVelocity.y > 0f)
                currentVelocity.y = 0f;
        }

        transform.position = pos;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position + Vector3.up * 10f;
        currentVelocity = Vector3.zero;
    }
}