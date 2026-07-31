using UnityEngine;
using Mirror;

public class NetworkPickupItem : NetworkBehaviour
{
    [Header("Item Data")]
    [SerializeField] private string itemId = "stick";
    [SerializeField] private string itemName = "Палка";
    [SerializeField] private int amount = 1;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public int Amount => amount;

    /// <summary>
    /// Вызывается на сервере когда игрок подобрал предмет
    /// </summary>
    [Server]
    public void PickUp(NetworkPlayerInventory inventory)
    {
        if (inventory == null) return;

        // Добавляем в инвентарь игрока (на сервере)
        inventory.ServerAddItem(itemId, itemName, amount);

        // Уничтожаем предмет для всех
        NetworkServer.Destroy(gameObject);

        Debug.Log($"Server: {inventory.name} picked up {itemName}");
    }
}
