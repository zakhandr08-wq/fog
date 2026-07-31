using UnityEngine;
using Mirror;

public class ResourceSpawnerNetwork : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject stickPrefab;
    [SerializeField] private GameObject stonePrefab;

    [Header("Settings")]
    [SerializeField] private int stickCount = 15;
    [SerializeField] private int stoneCount = 10;
    [SerializeField] private float spawnRadius = 20f;

    private bool hasSpawned = false;

    private void Update()
    {
        if (NetworkServer.active && !hasSpawned)
        {
            hasSpawned = true;
            SpawnResources();
        }
    }

    private void SpawnResources()
    {
        SpawnBatch(stickPrefab, stickCount);
        SpawnBatch(stonePrefab, stoneCount);

        Debug.Log($"[ResourceSpawner] Spawned {stickCount} sticks, " +
            $"{stoneCount} stones");
    }

    private void SpawnBatch(GameObject prefab, int count)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle * spawnRadius;

            Vector3 pos = new Vector3(
                randomCircle.x, 0.5f, randomCircle.y);

            GameObject item = Instantiate(
                prefab, pos, Quaternion.identity);

            NetworkServer.Spawn(item);
        }
    }
}