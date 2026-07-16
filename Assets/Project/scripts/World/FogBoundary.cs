using UnityEngine;

public class FogBoundary : MonoBehaviour
{
    private float blackoutTimer;
    [SerializeField] private float blackoutDuration = 0.15f;

    [Header("References")]
    [SerializeField] private Terrain terrain;

    [Header("Fog Zones")]
    [Tooltip("Расстояние от края terrain где начинается лёгкий туман")]
    [SerializeField] private float fogStartDistance = 30f;

    [Tooltip("Расстояние от края terrain где телепортирует")]
    [SerializeField] private float teleportLineDistance = 10f;

    [Header("Teleport")]
    [SerializeField] private float randomOffset = 15f;

    [Header("Sanity")]
    [SerializeField] private float lightFogSanityDrain = 2f;
    [SerializeField] private float heavyFogSanityDrain = 8f;

    [Header("Disorientation")]
    [SerializeField] private float blurDuration = 3f;

    [Header("Visual")]
    [SerializeField]
    private Color fogColor =
        new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField] private float maxFogDensity = 0.12f;

    [Header("Invisible Wall")]
    [SerializeField] private float hardBorderMargin = 2f;

    // Terrain bounds
    private float terrainMinX, terrainMaxX;
    private float terrainMinZ, terrainMaxZ;

    // Safe zone (no fog)
    private float safeMinX, safeMaxX;
    private float safeMinZ, safeMaxZ;

    // Teleport line
    private float teleportMinX, teleportMaxX;
    private float teleportMinZ, teleportMaxZ;

    // State
    private FogZone currentZone;
    private float fogDepth;
    private float fogIntensity;
    private float disoriented;

    // Cached
    private CharacterController playerController;
    private PlayerSanity playerSanity;
    private Transform playerTransform;

    // Original fog
    private bool originalFogEnabled;
    private Color originalFogColor;
    private float originalFogDensity;

    public enum FogZone
    {
        Safe,       // No fog
        LightFog,   // Can return
        Teleport    // Instant teleport
    }

    public bool IsPlayerInFog => currentZone != FogZone.Safe;
    public float FogIntensity => fogIntensity;
    public FogZone CurrentZone => currentZone;

    private void Start()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;

        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain != null)
        {
            CalculateBounds();
        }
        else
        {
            Debug.LogError("Terrain not found!");
            return;
        }

        originalFogEnabled = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerController =
                player.GetComponent<CharacterController>();
            playerSanity =
                player.GetComponent<PlayerSanity>();
            playerTransform = player.transform;
        }

        CreateInvisibleWalls();
    }

    private void CalculateBounds()
    {
        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        terrainMinX = tPos.x;
        terrainMaxX = tPos.x + tSize.x;
        terrainMinZ = tPos.z;
        terrainMaxZ = tPos.z + tSize.z;

        // Safe zone border (fog starts here)
        safeMinX = terrainMinX + fogStartDistance;
        safeMaxX = terrainMaxX - fogStartDistance;
        safeMinZ = terrainMinZ + fogStartDistance;
        safeMaxZ = terrainMaxZ - fogStartDistance;

        // Teleport line (instant teleport here)
        teleportMinX = terrainMinX + teleportLineDistance;
        teleportMaxX = terrainMaxX - teleportLineDistance;
        teleportMinZ = terrainMinZ + teleportLineDistance;
        teleportMaxZ = terrainMaxZ - teleportLineDistance;

        Debug.Log($"Safe zone: ({safeMinX},{safeMinZ}) " +
            $"to ({safeMaxX},{safeMaxZ})");
        Debug.Log($"Teleport line: {teleportLineDistance}m " +
            $"from edge");
        Debug.Log($"Fog zone: {fogStartDistance - teleportLineDistance}m " +
            $"of walkable fog");
    }

    private void CreateInvisibleWalls()
    {
        float height = 50f;
        float thickness = 1f;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        float cx = tPos.x + tSize.x / 2f;
        float cz = tPos.z + tSize.z / 2f;
        float y = tPos.y + height / 2f;

        CreateWallCollider("Wall_North",
            new Vector3(cx, y, terrainMaxZ),
            new Vector3(tSize.x, height, thickness));

        CreateWallCollider("Wall_South",
            new Vector3(cx, y, terrainMinZ),
            new Vector3(tSize.x, height, thickness));

        CreateWallCollider("Wall_East",
            new Vector3(terrainMaxX, y, cz),
            new Vector3(thickness, height, tSize.z));

        CreateWallCollider("Wall_West",
            new Vector3(terrainMinX, y, cz),
            new Vector3(thickness, height, tSize.z));
    }

    private void CreateWallCollider(
        string name, Vector3 pos, Vector3 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(transform);
        wall.transform.position = pos;
        var col = wall.AddComponent<BoxCollider>();
        col.size = size;
    }

    private void Update()
    {
        if (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.deltaTime;
        }

        if (playerController == null) return;

        UpdateZone();
        HandleCurrentZone();

        if (disoriented > 0f)
            disoriented -= Time.deltaTime;

        UpdateFogVisuals();
    }

    private void UpdateZone()
    {
        Vector3 pos = playerTransform.position;

        // Check if past teleport line
        bool pastTeleport =
            pos.x < teleportMinX || pos.x > teleportMaxX ||
            pos.z < teleportMinZ || pos.z > teleportMaxZ;

        // Check if in fog but before teleport line
        bool inFog =
            pos.x < safeMinX || pos.x > safeMaxX ||
            pos.z < safeMinZ || pos.z > safeMaxZ;

        FogZone previousZone = currentZone;

        if (pastTeleport)
        {
            currentZone = FogZone.Teleport;
        }
        else if (inFog)
        {
            currentZone = FogZone.LightFog;
        }
        else
        {
            currentZone = FogZone.Safe;
        }

        // Log zone changes
        if (currentZone != previousZone)
        {
            switch (currentZone)
            {
                case FogZone.Safe:
                    Debug.Log("Безопасная зона");
                    break;
                case FogZone.LightFog:
                    Debug.Log("Входишь в туман...");
                    break;
                case FogZone.Teleport:
                    Debug.Log("Слишком глубоко!");
                    break;
            }
        }

        // Calculate fog depth and intensity
        if (currentZone != FogZone.Safe)
        {
            fogDepth = GetFogDepth(pos);
            float maxDepth = fogStartDistance - teleportLineDistance;
            fogIntensity = Mathf.Clamp01(fogDepth / maxDepth);
        }
        else
        {
            fogDepth = 0f;
            fogIntensity = 0f;
        }
    }

    private void HandleCurrentZone()
    {
        switch (currentZone)
        {
            case FogZone.Safe:
                // Nothing
                break;

            case FogZone.LightFog:
                // Slow sanity drain
                if (playerSanity != null)
                {
                    playerSanity.DrainSanity(
                        lightFogSanityDrain * Time.deltaTime);
                }
                break;

            case FogZone.Teleport:
                // Instant teleport!
                TeleportPlayer();
                break;
        }
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

    private void TeleportPlayer()
    {
        Vector3 currentPos = playerTransform.position;
        Vector3 newPos = currentPos;

        float depthLeft = teleportMinX - currentPos.x;
        float depthRight = currentPos.x - teleportMaxX;
        float depthBottom = teleportMinZ - currentPos.z;
        float depthTop = currentPos.z - teleportMaxZ;

        float maxDepth = Mathf.Max(
            depthLeft, depthRight, depthBottom, depthTop);

        // Спавн на 1м от линии телепорта с другой стороны
        float spawnOffset = 1f;

        if (maxDepth == depthLeft)
        {
            // Шёл влево → появляется справа, 
            // почти на линии телепорта
            newPos.x = teleportMaxX - spawnOffset;
            newPos.z = currentPos.z
                + Random.Range(-randomOffset, randomOffset);
        }
        else if (maxDepth == depthRight)
        {
            newPos.x = teleportMinX + spawnOffset;
            newPos.z = currentPos.z
                + Random.Range(-randomOffset, randomOffset);
        }
        else if (maxDepth == depthBottom)
        {
            newPos.z = teleportMaxZ - spawnOffset;
            newPos.x = currentPos.x
                + Random.Range(-randomOffset, randomOffset);
        }
        else if (maxDepth == depthTop)
        {
            newPos.z = teleportMinZ + spawnOffset;
            newPos.x = currentPos.x
                + Random.Range(-randomOffset, randomOffset);
        }

        // Clamp to terrain
        newPos.x = Mathf.Clamp(newPos.x,
            terrainMinX + hardBorderMargin + 1f,
            terrainMaxX - hardBorderMargin - 1f);
        newPos.z = Mathf.Clamp(newPos.z,
            terrainMinZ + hardBorderMargin + 1f,
            terrainMaxZ - hardBorderMargin - 1f);

        // Высота через Raycast
        Vector3 rayOrigin = new Vector3(
            newPos.x,
            terrain.transform.position.y + 100f,
            newPos.z);

        RaycastHit hit;
        if (Physics.Raycast(
            rayOrigin, Vector3.down, out hit, 200f))
        {
            newPos.y = hit.point.y;
        }
        else
        {
            newPos.y = terrain.SampleHeight(newPos)
                + terrain.transform.position.y;
        }

        // Телепорт
        playerController.enabled = false;
        playerTransform.position = newPos;
        playerController.enabled = true;

        StartCoroutine(EnableControllerNextFrame());

        // Лёгкая дезориентация
        disoriented = blurDuration * 0.3f;

        if (playerSanity != null)
        {
            playerSanity.DrainSanity(1f);
        }
    }
    private System.Collections.IEnumerator EnableControllerNextFrame()
    {
        // Ждём 2 кадра чтобы физика обновилась
        yield return null;
        yield return null;

        // Сбрасываем вертикальную скорость
        var pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetVerticalVelocity();
        }

        // Включаем контроллер
        playerController.enabled = true;
    }

    private void UpdateFogVisuals()
    {
        if (currentZone == FogZone.LightFog
            || currentZone == FogZone.Teleport)
        {
            // Усиливаем туман поверх базового
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;

            // Берём максимум из базового и зонального тумана
            float zoneDensity = Mathf.Lerp(
                originalFogDensity, maxFogDensity, fogIntensity);
            RenderSettings.fogDensity =
                Mathf.Max(originalFogDensity, zoneDensity);
        }
        else if (disoriented > 0f)
        {
            float t = disoriented / blurDuration;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(
                originalFogColor, fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(
                originalFogDensity,
                maxFogDensity * 0.4f, t);
        }
        else
        {
            // Возвращаем ТВОИ настройки из Lighting
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogColor = originalFogColor;
        }
    }

    /// <summary>
    /// Positive = in safe zone, Negative = in fog
    /// </summary>
    public float GetDistanceToFog(Vector3 position)
    {
        float distLeft = position.x - safeMinX;
        float distRight = safeMaxX - position.x;
        float distBottom = position.z - safeMinZ;
        float distTop = safeMaxZ - position.z;

        return Mathf.Min(
            distLeft, distRight,
            distBottom, distTop);
    }

    private void OnDrawGizmos()
    {
        if (terrain == null) return;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        float y = tPos.y + 3f;

        // Terrain bounds (white)
        Gizmos.color = Color.white;
        DrawRect(
            tPos.x, tPos.z,
            tPos.x + tSize.x, tPos.z + tSize.z, y);

        // Safe zone (green)
        float sMinX = tPos.x + fogStartDistance;
        float sMaxX = tPos.x + tSize.x - fogStartDistance;
        float sMinZ = tPos.z + fogStartDistance;
        float sMaxZ = tPos.z + tSize.z - fogStartDistance;

        Gizmos.color = Color.green;
        DrawRect(sMinX, sMinZ, sMaxX, sMaxZ, y);

        // Teleport line (red)
        float tpMinX = tPos.x + teleportLineDistance;
        float tpMaxX = tPos.x + tSize.x - teleportLineDistance;
        float tpMinZ = tPos.z + teleportLineDistance;
        float tpMaxZ = tPos.z + tSize.z - teleportLineDistance;

        Gizmos.color = Color.red;
        DrawRect(tpMinX, tpMinZ, tpMaxX, tpMaxZ, y + 0.1f);

        // Labels in scene view
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(sMinX, y + 2f, sMinZ),
            "← Безопасная зона →");
        UnityEditor.Handles.Label(
            new Vector3(tpMinX - 5f, y + 2f, tpMinZ),
            "ТУМАН");
        UnityEditor.Handles.Label(
            new Vector3(tPos.x + 2f, y + 2f, tPos.z + 2f),
            "ТЕЛЕПОРТ");
#endif

        // Fog zone fill (yellow transparent)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        // North
        DrawFilledRect(sMinX, sMaxZ, sMaxX,
            tPos.z + tSize.z - teleportLineDistance, y);
        // South
        DrawFilledRect(sMinX,
            tPos.z + teleportLineDistance, sMaxX, sMinZ, y);

        // Teleport zone fill (red transparent)
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        // North
        DrawFilledRect(tpMinX, tpMaxZ, tpMaxX,
            tPos.z + tSize.z, y);
        // South
        DrawFilledRect(tpMinX, tPos.z, tpMaxX, tpMinZ, y);
        // East
        DrawFilledRect(tpMaxX, tPos.z,
            tPos.x + tSize.x, tPos.z + tSize.z, y);
        // West
        DrawFilledRect(tPos.x, tPos.z, tpMinX,
            tPos.z + tSize.z, y);
    }

    private void DrawRect(
        float minX, float minZ,
        float maxX, float maxZ, float y)
    {
        Vector3 a = new Vector3(minX, y, minZ);
        Vector3 b = new Vector3(maxX, y, minZ);
        Vector3 c = new Vector3(maxX, y, maxZ);
        Vector3 d = new Vector3(minX, y, maxZ);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    private void DrawFilledRect(
        float minX, float minZ,
        float maxX, float maxZ, float y)
    {
        Vector3 center = new Vector3(
            (minX + maxX) / 2f, y,
            (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(
            maxX - minX, 0.1f, maxZ - minZ);
        Gizmos.DrawCube(center, size);
    }
}