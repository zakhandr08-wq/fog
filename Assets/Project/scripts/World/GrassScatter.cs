using UnityEngine;

public class GrassScatter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Terrain terrain;

    [Header("Grass")]
    [SerializeField] private GameObject[] grassPrefabs;
    [SerializeField] private int grassCount = 500;
    [SerializeField] private float grassScaleMin = 0.8f;
    [SerializeField] private float grassScaleMax = 1.3f;

    [Header("Bushes")]
    [SerializeField] private GameObject[] bushPrefabs;
    [SerializeField] private int bushCount = 100;
    [SerializeField] private float bushScaleMin = 0.7f;
    [SerializeField] private float bushScaleMax = 1.5f;

    [Header("Map Margins")]
    [SerializeField] private float edgeMargin = 35f;

    [Header("Placement Rules")]
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float minDistanceToObstacle = 1.5f;
    [SerializeField] private float minGrassToGrass = 0.6f;
    [SerializeField] private float minBushToBush = 3f;
    [SerializeField] private float minBushToGrass = 1f;
    [SerializeField] private float maxSlope = 28f;

    [Header("Dense Edges")]
    [SerializeField] private bool denseNearFog = true;
    [SerializeField] private float fogZoneStart = 30f;
    [SerializeField] private float edgeDensityMultiplier = 3f;

    [Header("Masks")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Max Attempts")]
    [SerializeField] private int maxAttempts = 8000;

    // Runtime
    private Transform grassParent;
    private Transform bushParent;
    private Vector3[] allPositions;
    private PlantType[] allTypes;
    private int placedCount;

    private enum PlantType
    {
        Grass,
        Bush
    }

    private void Start()
    {
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("GrassScatter: Terrain not found!");
            return;
        }

        // Create parents
        GameObject grassRoot = new GameObject("Grass_Runtime");
        grassRoot.transform.SetParent(transform);
        grassParent = grassRoot.transform;

        GameObject bushRoot = new GameObject("Bushes_Runtime");
        bushRoot.transform.SetParent(transform);
        bushParent = bushRoot.transform;

        int totalCount = grassCount + bushCount;
        allPositions = new Vector3[totalCount];
        allTypes = new PlantType[totalCount];
        placedCount = 0;

        // Spawn in order: bushes first (they need more space)
        if (bushPrefabs != null && bushPrefabs.Length > 0)
        {
            SpawnPlants(bushPrefabs, bushCount,
                bushScaleMin, bushScaleMax,
                bushParent, PlantType.Bush);
        }

        if (grassPrefabs != null && grassPrefabs.Length > 0)
        {
            SpawnPlants(grassPrefabs, grassCount,
                grassScaleMin, grassScaleMax,
                grassParent, PlantType.Grass);
        }

        Debug.Log($"Spawned: {placedCount} plants total");
    }

    private void SpawnPlants(
        GameObject[] prefabs, int count,
        float scaleMin, float scaleMax,
        Transform parent, PlantType type)
    {
        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        float minX = tPos.x + edgeMargin;
        float maxX = tPos.x + tSize.x - edgeMargin;
        float minZ = tPos.z + edgeMargin;
        float maxZ = tPos.z + tSize.z - edgeMargin;

        // If dense near fog, also spawn in fog zone
        float realMinX = denseNearFog ? tPos.x + 5f : minX;
        float realMaxX = denseNearFog ? tPos.x + tSize.x - 5f : maxX;
        float realMinZ = denseNearFog ? tPos.z + 5f : minZ;
        float realMaxZ = denseNearFog ? tPos.z + tSize.z - 5f : maxZ;

        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            float x, z;

            // Chance to spawn near edges if denseNearFog
            if (denseNearFog && Random.value < 0.4f)
            {
                // Spawn in fog zone (near edges)
                int side = Random.Range(0, 4);
                switch (side)
                {
                    case 0: // North
                        x = Random.Range(realMinX, realMaxX);
                        z = Random.Range(
                            tPos.z + tSize.z - fogZoneStart,
                            realMaxZ);
                        break;
                    case 1: // South
                        x = Random.Range(realMinX, realMaxX);
                        z = Random.Range(realMinZ,
                            tPos.z + fogZoneStart);
                        break;
                    case 2: // East
                        x = Random.Range(
                            tPos.x + tSize.x - fogZoneStart,
                            realMaxX);
                        z = Random.Range(realMinZ, realMaxZ);
                        break;
                    default: // West
                        x = Random.Range(realMinX,
                            tPos.x + fogZoneStart);
                        z = Random.Range(realMinZ, realMaxZ);
                        break;
                }
            }
            else
            {
                x = Random.Range(minX, maxX);
                z = Random.Range(minZ, maxZ);
            }

            Vector3 pos = new Vector3(x, 0f, z);
            float terrainHeight = terrain.SampleHeight(pos)
                + terrain.transform.position.y;
            pos.y = terrainHeight + yOffset;

            if (!IsValidPosition(pos, type))
                continue;

            // Spawn
            GameObject prefab = prefabs[
                Random.Range(0, prefabs.Length)];

            GameObject plant = Instantiate(
                prefab, pos, Quaternion.identity, parent);

            float scale = Random.Range(scaleMin, scaleMax);
            plant.transform.localScale = Vector3.one * scale;
            plant.transform.rotation = Quaternion.Euler(
                0f, Random.Range(0f, 360f), 0f);

            allPositions[placedCount] = pos;
            allTypes[placedCount] = type;
            placedCount++;
            spawned++;
        }

        string typeName = type == PlantType.Grass
            ? "grass" : "bushes";
        Debug.Log($"Placed {spawned}/{count} {typeName}" +
            $" after {attempts} attempts");
    }

    private bool IsValidPosition(Vector3 pos, PlantType type)
    {
        // 1. Slope check
        if (GetTerrainSlope(pos) > maxSlope)
            return false;

        // 2. Obstacle check
        if (Physics.CheckSphere(
            pos + Vector3.up * 0.3f,
            minDistanceToObstacle,
            obstacleMask))
            return false;

        // 3. Distance to other plants
        float minDist = GetMinDistance(type);

        for (int i = 0; i < placedCount; i++)
        {
            float dist = Vector3.Distance(
                new Vector3(pos.x, 0f, pos.z),
                new Vector3(allPositions[i].x, 0f,
                    allPositions[i].z));

            // Bush to bush needs more space
            if (type == PlantType.Bush
                && allTypes[i] == PlantType.Bush)
            {
                if (dist < minBushToBush) return false;
            }
            // Bush to grass
            else if (type == PlantType.Bush
                || allTypes[i] == PlantType.Bush)
            {
                if (dist < minBushToGrass) return false;
            }
            // Grass to grass
            else
            {
                if (dist < minGrassToGrass) return false;
            }
        }

        return true;
    }

    private float GetMinDistance(PlantType type)
    {
        return type == PlantType.Bush
            ? minBushToBush : minGrassToGrass;
    }

    private float GetTerrainSlope(Vector3 worldPos)
    {
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float normX = Mathf.Clamp01(
            (worldPos.x - terrainPos.x) / terrainSize.x);
        float normZ = Mathf.Clamp01(
            (worldPos.z - terrainPos.z) / terrainSize.z);

        Vector3 normal = terrain.terrainData
            .GetInterpolatedNormal(normX, normZ);
        return Vector3.Angle(normal, Vector3.up);
    }

    private void OnDrawGizmosSelected()
    {
        if (terrain == null) return;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        // Safe zone
        float y = tPos.y + 2f;
        Gizmos.color = Color.green;
        Vector3 center = tPos
            + new Vector3(tSize.x / 2f, 2f, tSize.z / 2f);
        Vector3 size = new Vector3(
            tSize.x - edgeMargin * 2f,
            0.1f,
            tSize.z - edgeMargin * 2f);
        Gizmos.DrawWireCube(center, size);
    }
}