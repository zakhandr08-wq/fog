using UnityEngine;

public class ResourceSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Terrain terrain;

    [Header("Sticks")]
    [SerializeField] private GameObject stickPrefab;
    [SerializeField] private int stickCount = 30;

    [Header("Stones")]
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private int stoneCount = 20;

    [Header("Spawn Settings")]
    [SerializeField] private float edgeMargin = 40f;
    [SerializeField] private float spawnHeight = 2f;
    [SerializeField] private float minDistanceBetween = 3f;
    [SerializeField] private float minDistanceToObstacle = 2f;
    [SerializeField] private float maxSlope = 25f;

    [Header("Masks")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Max Attempts")]
    [SerializeField] private int maxAttemptsPerItem = 50;

    private Vector3[] spawnedPositions;
    private int spawnedCount;
    private Transform resourceParent;

    private void Start()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("ResourceSpawner: Terrain not found!");
            return;
        }

        // Создаём родителя для порядка в Hierarchy
        GameObject parent = new GameObject("Resources_Runtime");
        parent.transform.SetParent(transform);
        resourceParent = parent.transform;

        int totalCount = stickCount + stoneCount;
        spawnedPositions = new Vector3[totalCount];
        spawnedCount = 0;

        SpawnResources(stickPrefab, stickCount, "Stick");
        SpawnResources(stonePrefab, stoneCount, "Stone");

        Debug.Log($"Resources spawned: {spawnedCount} total");
    }

    private void SpawnResources(
        GameObject prefab, int count, string name)
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                $"ResourceSpawner: {name} prefab not assigned!");
            return;
        }

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        float minX = tPos.x + edgeMargin;
        float maxX = tPos.x + tSize.x - edgeMargin;
        float minZ = tPos.z + edgeMargin;
        float maxZ = tPos.z + tSize.z - edgeMargin;

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            for (int attempt = 0;
                attempt < maxAttemptsPerItem; attempt++)
            {
                float x = Random.Range(minX, maxX);
                float z = Random.Range(minZ, maxZ);

                Vector3 pos = new Vector3(x, 0f, z);

                // Получаем высоту terrain
                float terrainY = terrain.SampleHeight(pos)
                    + tPos.y;

                // Спавним чуть выше чтобы упали на землю
                pos.y = terrainY + spawnHeight;

                if (!IsValidPosition(pos))
                    continue;

                // Спавним
                var obj = Instantiate(
                    prefab, pos,
                    Quaternion.Euler(
                        Random.Range(-10f, 10f),
                        Random.Range(0f, 360f),
                        Random.Range(-10f, 10f)),
                    resourceParent);

                obj.name = $"{name}_{spawned}";

                spawnedPositions[spawnedCount] = pos;
                spawnedCount++;
                spawned++;
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning(
                    $"Could not place {name}_{i}");
            }
        }

        Debug.Log($"Placed {spawned}/{count} {name}s");
    }

    private bool IsValidPosition(Vector3 pos)
    {
        // 1. Проверка уклона
        if (GetTerrainSlope(pos) > maxSlope)
            return false;

        // 2. Проверка препятствий
        if (Physics.CheckSphere(
            pos, minDistanceToObstacle, obstacleMask))
            return false;

        // 3. Проверка расстояния до других ресурсов
        for (int i = 0; i < spawnedCount; i++)
        {
            float dist = Vector3.Distance(
                new Vector3(pos.x, 0f, pos.z),
                new Vector3(
                    spawnedPositions[i].x, 0f,
                    spawnedPositions[i].z));

            if (dist < minDistanceBetween)
                return false;
        }

        return true;
    }

    private float GetTerrainSlope(Vector3 worldPos)
    {
        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        float normX = Mathf.Clamp01(
            (worldPos.x - tPos.x) / tSize.x);
        float normZ = Mathf.Clamp01(
            (worldPos.z - tPos.z) / tSize.z);

        Vector3 normal = terrain.terrainData
            .GetInterpolatedNormal(normX, normZ);

        return Vector3.Angle(normal, Vector3.up);
    }
}