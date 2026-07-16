using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float placeDistance = 8f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Ghost Materials")]
    [SerializeField] private Material ghostValidMaterial;
    [SerializeField] private Material ghostInvalidMaterial;

    // State
    private GameObject ghostObject;
    private GameObject prefabToPlace;
    private bool isPlacing;
    private bool isValidPosition;
    private float ghostRotation;

    // Components
    private Camera playerCamera;
    private PlayerController playerController;

    // Events
    public System.Action OnPlacementComplete;
    public System.Action OnPlacementCancelled;

    public bool IsPlacing => isPlacing;

    private void Start()
    {
        playerCamera = Camera.main;
        playerController =
            FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (!isPlacing) return;

        UpdateGhostPosition();
        CheckValidPosition();
        UpdateGhostMaterial();
        HandleInput();
    }

    /// <summary>
    /// Start placement mode with a prefab
    /// </summary>
    public void StartPlacement(GameObject prefab)
    {
        if (isPlacing) CancelPlacement();

        prefabToPlace = prefab;
        isPlacing = true;
        ghostRotation = 0f;

        // Create ghost
        CreateGhost(prefab);

        Debug.Log("Режим размещения: Enter — поставить, " +
            "Esc — отмена, Q/E — вращать");
    }

    private void CreateGhost(GameObject prefab)
    {
        // Instantiate ghost copy
        ghostObject = Instantiate(prefab);
        ghostObject.name = "PlacementGhost";

        // Disable all scripts on ghost
        var scripts = ghostObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != null)
                script.enabled = false;
        }

        // Disable colliders (so ghost doesn't block)
        var colliders = ghostObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Disable lights
        var lights = ghostObject.GetComponentsInChildren<Light>();
        foreach (var light in lights)
        {
            light.enabled = false;
        }

        // Disable particles
        var particles = ghostObject
            .GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Stop();
            ps.gameObject.SetActive(false);
        }

        // Make semi-transparent
        SetGhostMaterial(ghostValidMaterial);
    }

    private void UpdateGhostPosition()
    {
        if (ghostObject == null) return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        Vector3 targetPos;

        if (Physics.Raycast(
            ray, out RaycastHit hit,
            placeDistance, groundLayer))
        {
            targetPos = hit.point;
        }
        else
        {
            // If no ground hit, place at max distance
            targetPos = playerCamera.transform.position
                + playerCamera.transform.forward * placeDistance;
            targetPos.y = 0f;
        }

        // Smooth movement
        ghostObject.transform.position = Vector3.Lerp(
            ghostObject.transform.position,
            targetPos,
            Time.deltaTime * 15f
        );

        // Rotation
        ghostObject.transform.rotation =
            Quaternion.Euler(0f, ghostRotation, 0f);
    }

    private void CheckValidPosition()
    {
        if (ghostObject == null)
        {
            isValidPosition = false;
            return;
        }

        // Check for obstacles using OverlapSphere
        float checkRadius = 0.5f;
        Vector3 checkPos = ghostObject.transform.position
            + Vector3.up * 0.3f;

        Collider[] hits = Physics.OverlapSphere(
            checkPos, checkRadius, obstacleLayer);

        isValidPosition = hits.Length == 0;

        // Also check if too far
        float dist = Vector3.Distance(
            playerCamera.transform.position,
            ghostObject.transform.position
        );

        if (dist > placeDistance + 1f)
        {
            isValidPosition = false;
        }

        // Check if on ground
        Ray downRay = new Ray(
            ghostObject.transform.position + Vector3.up,
            Vector3.down
        );

        if (!Physics.Raycast(downRay, 3f, groundLayer))
        {
            isValidPosition = false;
        }
    }

    private void UpdateGhostMaterial()
    {
        if (ghostObject == null) return;

        Material mat = isValidPosition
            ? ghostValidMaterial
            : ghostInvalidMaterial;

        SetGhostMaterial(mat);
    }

    private void SetGhostMaterial(Material mat)
    {
        if (mat == null) return;

        var renderers = ghostObject
            .GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            renderer.materials = mats;
        }
    }

    private void HandleInput()
    {
        // Rotate with Q/E
        if (Input.GetKey(KeyCode.Q))
        {
            ghostRotation -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            ghostRotation += rotationSpeed * Time.deltaTime;
        }

        // Place with Enter or Left Click
        if (Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0))
        {
            if (isValidPosition)
            {
                ConfirmPlacement();
            }
            else
            {
                Debug.Log("Нельзя поставить здесь!");
            }
        }

        // Cancel with Escape or Right Click
        if (Input.GetKeyDown(KeyCode.Escape)
            || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    private void ConfirmPlacement()
    {
        Vector3 pos = ghostObject.transform.position;
        Quaternion rot = ghostObject.transform.rotation;

        // Destroy ghost
        Destroy(ghostObject);
        ghostObject = null;

        // Spawn real object
        Instantiate(prefabToPlace, pos, rot);
        Debug.Log($"Построено: {prefabToPlace.name}");

        // Clean up
        prefabToPlace = null;
        isPlacing = false;

        OnPlacementComplete?.Invoke();
    }

    public void CancelPlacement()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }

        prefabToPlace = null;
        isPlacing = false;

        Debug.Log("Размещение отменено");
        OnPlacementCancelled?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (!isPlacing || ghostObject == null) return;

        Gizmos.color = isValidPosition
            ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            ghostObject.transform.position + Vector3.up * 0.3f,
            0.5f
        );
    }
}
