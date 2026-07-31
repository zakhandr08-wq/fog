using UnityEngine;
using Mirror;

public class NetworkPlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private NetworkPickupItem currentTarget;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (playerCamera == null) return;

        ScanForInteractable();

        if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
        {
            // Отправляем команду серверу с сетевым идентификатором цели
            CmdPickupItem(currentTarget.netId);
        }
    }

    private void ScanForInteractable()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward);

        if (Physics.Raycast(
            ray, out RaycastHit hit,
            interactionRange, interactableLayer))
        {
            var pickup = hit.collider
                .GetComponentInParent<NetworkPickupItem>();

            if (pickup != currentTarget)
            {
                currentTarget = pickup;

                if (currentTarget != null)
                    Debug.Log($"Looking at: {currentTarget.ItemName} [E]");
            }
        }
        else
        {
            currentTarget = null;
        }
    }

    /// <summary>
    /// Клиент просит сервер подобрать предмет
    /// </summary>
    [Command]
    private void CmdPickupItem(uint targetNetId)
    {
        // Ищем объект по сетевому ID (на сервере!)
        if (!NetworkServer.spawned.TryGetValue(
            targetNetId, out NetworkIdentity targetIdentity))
        {
            Debug.LogWarning($"Server: item {targetNetId} not found");
            return;
        }

        var pickup = targetIdentity
            .GetComponent<NetworkPickupItem>();

        if (pickup == null)
        {
            Debug.LogWarning("Server: not a pickup item");
            return;
        }

        // Проверяем дистанцию (защита от читерства)
        float dist = Vector3.Distance(
            transform.position, pickup.transform.position);

        if (dist > interactionRange + 1f)
        {
            Debug.LogWarning($"Server: {name} too far to pickup");
            return;
        }

        // Получаем инвентарь игрока
        var inventory = GetComponent<NetworkPlayerInventory>();
        if (inventory == null) return;

        // Выполняем подбор на сервере
        pickup.PickUp(inventory);
    }
}