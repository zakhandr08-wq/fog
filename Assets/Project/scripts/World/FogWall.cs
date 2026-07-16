using UnityEngine;

public class FogWall : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Terrain terrain;

    [Header("Settings")]
    [SerializeField] private float fogStartDistance = 30f;
    [SerializeField] private float wallHeight = 30f;

    [Header("Visual")]
    [SerializeField] private Material fogWallMaterial;

    private void Start()
    {
        if (terrain == null || fogWallMaterial == null)
        {
            Debug.LogError("Terrain or Material not set!");
            return;
        }

        CreateFogWalls();
    }

    private void CreateFogWalls()
    {
        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;

        float innerMinX = tPos.x + fogStartDistance;
        float innerMaxX = tPos.x + tSize.x - fogStartDistance;
        float innerMinZ = tPos.z + fogStartDistance;
        float innerMaxZ = tPos.z + tSize.z - fogStartDistance;

        float fogWidth = fogStartDistance;

        // Стены подняты чуть выше земли
        float bottomOffset = 0.3f;
        float y = tPos.y + bottomOffset + wallHeight / 2f;

        // === СТЕНЫ ===

        CreateWall("FogWall_North",
            new Vector3(
                tPos.x + tSize.x / 2f,
                y,
                innerMaxZ + fogWidth / 2f),
            new Vector3(tSize.x, wallHeight, fogWidth));

        CreateWall("FogWall_South",
            new Vector3(
                tPos.x + tSize.x / 2f,
                y,
                innerMinZ - fogWidth / 2f),
            new Vector3(tSize.x, wallHeight, fogWidth));

        CreateWall("FogWall_East",
            new Vector3(
                innerMaxX + fogWidth / 2f,
                y,
                tPos.z + tSize.z / 2f),
            new Vector3(fogWidth, wallHeight, tSize.z));

        CreateWall("FogWall_West",
            new Vector3(
                innerMinX - fogWidth / 2f,
                y,
                tPos.z + tSize.z / 2f),
            new Vector3(fogWidth, wallHeight, tSize.z));

        // === ПОЛ ТУМАНА ===

        float groundY = tPos.y + 0.05f;

        CreateGroundFog("FogGround_North",
            new Vector3(
                tPos.x + tSize.x / 2f,
                groundY,
                innerMaxZ + fogWidth / 2f),
            new Vector2(tSize.x, fogWidth));

        CreateGroundFog("FogGround_South",
            new Vector3(
                tPos.x + tSize.x / 2f,
                groundY,
                innerMinZ - fogWidth / 2f),
            new Vector2(tSize.x, fogWidth));

        CreateGroundFog("FogGround_East",
            new Vector3(
                innerMaxX + fogWidth / 2f,
                groundY,
                tPos.z + tSize.z / 2f),
            new Vector2(fogWidth, tSize.z));

        CreateGroundFog("FogGround_West",
            new Vector3(
                innerMinX - fogWidth / 2f,
                groundY,
                tPos.z + tSize.z / 2f),
            new Vector2(fogWidth, tSize.z));
    }

    private void CreateWall(
        string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(transform);
        wall.transform.position = pos;
        wall.transform.localScale = scale;

        Destroy(wall.GetComponent<Collider>());

        var renderer = wall.GetComponent<Renderer>();
        renderer.material = fogWallMaterial;
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void CreateGroundFog(
        string name, Vector3 pos, Vector2 size)
    {
        var ground = GameObject.CreatePrimitive(
            PrimitiveType.Quad);
        ground.name = name;
        ground.transform.SetParent(transform);
        ground.transform.position = pos;

        // Quad по умолчанию стоит вертикально
        // Поворачиваем горизонтально
        ground.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f);
        ground.transform.localScale =
            new Vector3(size.x, size.y, 1f);

        Destroy(ground.GetComponent<Collider>());

        var renderer = ground.GetComponent<Renderer>();
        renderer.material = fogWallMaterial;
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}