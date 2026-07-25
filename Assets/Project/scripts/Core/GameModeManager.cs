using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Player,
        Entity,
        Seeker
    }

    [Header("Objects")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject entityObject;
    [SerializeField] private GameObject seekerObject;

    [Header("HUDs")]
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject entityHUD;
    [SerializeField] private GameObject seekerHUD;

    [Header("Fog Systems")]
    [SerializeField] private FogBoundary fogBoundary;

    [Header("Keys")]
    [SerializeField] private KeyCode playerKey = KeyCode.F1;
    [SerializeField] private KeyCode entityKey = KeyCode.F2;
    [SerializeField] private KeyCode seekerKey = KeyCode.F3;

    private GameMode currentMode = GameMode.Player;

    public GameMode CurrentMode => currentMode;

    private void Start()
    {
        SetMode(GameMode.Player);
    }

    private void Update()
    {
        if (DebugConsole.IsOpen) return;

        if (Input.GetKeyDown(playerKey))
            SetMode(GameMode.Player);
        else if (Input.GetKeyDown(entityKey))
            SetMode(GameMode.Entity);
        else if (Input.GetKeyDown(seekerKey))
            SetMode(GameMode.Seeker);
    }

    public void ToggleMode()
    {
        switch (currentMode)
        {
            case GameMode.Player: SetMode(GameMode.Entity); break;
            case GameMode.Entity: SetMode(GameMode.Seeker); break;
            case GameMode.Seeker: SetMode(GameMode.Player); break;
        }
    }

    public void SetMode(GameMode mode)
    {
        currentMode = mode;

        Vector3 playerPos = playerObject != null
            ? playerObject.transform.position
            : Vector3.zero;

        // === Настроить каждого — контроль + камера ===
        ApplyMode(playerObject, mode == GameMode.Player);
        ApplyMode(entityObject, mode == GameMode.Entity);
        ApplyMode(seekerObject, mode == GameMode.Seeker);

        // === Для сущности: телепорт к игроку при активации ===
        if (mode == GameMode.Entity && entityObject != null)
        {
            var entity = entityObject
                .GetComponent<EntityController>();
            if (entity != null)
                entity.TeleportTo(playerPos);
        }

        // === UI ===
        if (playerHUD != null)
            playerHUD.SetActive(mode == GameMode.Player);
        if (entityHUD != null)
            entityHUD.SetActive(mode == GameMode.Entity);
        if (seekerHUD != null)
            seekerHUD.SetActive(mode == GameMode.Seeker);

        // === Fog — только для игрока ===
        if (fogBoundary != null)
            fogBoundary.enabled = (mode == GameMode.Player);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"=== Mode: {mode} ===");
    }

    /// <summary>
    /// Включить/выключить управление объектом без его скрытия
    /// </summary>
    private void ApplyMode(GameObject obj, bool active)
    {
        if (obj == null) return;

        // Убедиться что объект активен
        if (!obj.activeSelf)
            obj.SetActive(true);

        if (obj == null) return;

        // ВАЖНО: сам объект НЕ отключаем!
        // Он должен продолжать существовать в мире.

        // === Игрок ===
        var pc = obj.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = active;

        var pi = obj.GetComponent<PlayerInteraction>();
        if (pi != null) pi.enabled = active;

        // === Сущность ===
        var ec = obj.GetComponent<EntityController>();
        if (ec != null) ec.enabled = active;

        var ea = obj.GetComponentInChildren<EntityAbilitiesManager>();
        if (ea != null) ea.enabled = active;

        // === Ищейка ===
        var sc = obj.GetComponent<SeekerController>();
        if (sc != null) sc.enabled = active;

        // === Камера ===
        var cam = obj.GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cam.gameObject.SetActive(active);
            cam.tag = active ? "MainCamera" : "Untagged";

            var listener = cam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }
    }
}