using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    [Header("Modes")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject entityObject;

    [Header("UI")]
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject entityHUD;

    [Header("Fog Systems")]
    [SerializeField] private FogBoundary fogBoundary;

    [Header("Switch Key")]
    [SerializeField] private KeyCode switchKey = KeyCode.F1;

    private bool isEntityMode;

    private void Start()
    {
        SetPlayerMode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        if (isEntityMode)
            SetPlayerMode();
        else
            SetEntityMode();
    }

    private void SetPlayerMode()
    {
        isEntityMode = false;

        // === Èãðîê ===
        if (playerObject != null)
        {
            var pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = true;

            var pi = playerObject.GetComponent<PlayerInteraction>();
            if (pi != null) pi.enabled = true;

            var pCam = playerObject.GetComponentInChildren<Camera>(true);
            if (pCam != null)
            {
                pCam.gameObject.SetActive(true);
                pCam.tag = "MainCamera";

                var listener = pCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
        }

        // === Ñóùíîñòü ===
        if (entityObject != null)
        {
            var eCam = entityObject.GetComponentInChildren<Camera>(true);
            if (eCam != null)
            {
                eCam.tag = "Untagged";

                var listener = eCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            entityObject.SetActive(false);
        }

        // === UI ===
        if (playerHUD != null) playerHUD.SetActive(true);
        if (entityHUD != null) entityHUD.SetActive(false);

        // === Fog ===
        if (fogBoundary != null) fogBoundary.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("=== Ðåæèì: ÂÛÆÈÂØÈÉ ===");
    }

    private void SetEntityMode()
    {
        isEntityMode = true;

        Vector3 playerPos = Vector3.zero;

        // === Èãðîê ===
        if (playerObject != null)
        {
            playerPos = playerObject.transform.position;

            var pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            var pi = playerObject.GetComponent<PlayerInteraction>();
            if (pi != null) pi.enabled = false;

            var pCam = playerObject.GetComponentInChildren<Camera>(true);
            if (pCam != null)
            {
                pCam.tag = "Untagged";

                var listener = pCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;

                pCam.gameObject.SetActive(false);
            }
        }

        // === Ñóùíîñòü ===
        if (entityObject != null)
        {
            entityObject.SetActive(true);

            var eCam = entityObject.GetComponentInChildren<Camera>(true);
            if (eCam != null)
            {
                eCam.tag = "MainCamera";

                var listener = eCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            var entityController = entityObject
                .GetComponent<EntityController>();
            if (entityController != null)
                entityController.TeleportTo(playerPos);
        }

        // === UI ===
        if (playerHUD != null) playerHUD.SetActive(false);
        if (entityHUD != null) entityHUD.SetActive(true);

        // === Fog ===
        if (fogBoundary != null) fogBoundary.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("=== Ðåæèì: ÑÓÙÍÎÑÒÜ ===");
    }
}