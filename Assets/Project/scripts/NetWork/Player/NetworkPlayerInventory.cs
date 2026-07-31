using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class NetworkPlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSlots = 6;

    // === СИНХРОНИЗИРУЕМЫЙ СПИСОК ===
    private readonly SyncList<InventoryItem> items =
        new SyncList<InventoryItem>();

    // События для UI
    public event Action OnInventoryChanged;

    // Публичный доступ к списку
    public SyncList<InventoryItem> Items => items;

    [System.Serializable]
    public struct InventoryItem
    {
        public string itemId;
        public string itemName;
        public int amount;
    }

    // ====================================
    // Регистрация обновлений
    // ====================================

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Подписываемся на изменения списка
        items.OnChange += OnItemsChanged;

        // Первичное обновление UI
        OnInventoryChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        items.OnChange -= OnItemsChanged;
    }

    private void OnItemsChanged(
        SyncList<InventoryItem>.Operation op,
        int index,
        InventoryItem oldItem)
    {
        Debug.Log($"Inventory changed on {name}: {op}");

        if (isLocalPlayer)
            OnInventoryChanged?.Invoke();
    }

    // ====================================
    // SERVER METHODS
    // ====================================

    [Server]
    public void ServerAddItem(string id, string itemName, int amount)
    {
        // Пытаемся стакнуть с существующим
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == id)
            {
                var item = items[i];
                item.amount += amount;
                items[i] = item;
                return;
            }
        }

        // Не нашли — добавляем новый
        if (items.Count < maxSlots)
        {
            items.Add(new InventoryItem
            {
                itemId = id,
                itemName = itemName,
                amount = amount
            });
        }
        else
        {
            Debug.Log($"Server: {name}'s inventory is full!");
        }
    }

    [Server]
    public bool ServerRemoveItem(string id, int amount)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == id && items[i].amount >= amount)
            {
                var item = items[i];
                item.amount -= amount;

                if (item.amount <= 0)
                    items.RemoveAt(i);
                else
                    items[i] = item;

                return true;
            }
        }

        return false;
    }

    [Server]
    public bool ServerHasItem(string id, int amount)
    {
        foreach (var item in items)
        {
            if (item.itemId == id && item.amount >= amount)
                return true;
        }
        return false;
    }

    public int GetCount(string id)
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item.itemId == id)
                total += item.amount;
        }
        return total;
    }

    // ====================================
    // CRAFTING (клиент → сервер)
    // ====================================

    [Command]
    public void CmdRequestCraft(string recipeName)
    {
        var craftingManager = NetworkCraftingManager.Instance;
        if (craftingManager == null)
        {
            Debug.LogWarning("Server: no NetworkCraftingManager");
            return;
        }

        craftingManager.ServerTryCraft(this, recipeName);
    }
}