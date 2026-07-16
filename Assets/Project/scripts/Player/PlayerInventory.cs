using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int amount;

        public bool IsEmpty => item == null;

        public void Clear()
        {
            item = null;
            amount = 0;
        }
    }

    [Header("Settings")]
    [SerializeField] private int mainSlots = 4;
    [SerializeField] private int pocketSlots = 2;

    // Data
    private InventorySlot[] mainInventory;
    private InventorySlot[] pockets;

    // Events
    public Action OnInventoryChanged;

    public InventorySlot[] MainInventory => mainInventory;
    public InventorySlot[] Pockets => pockets;

    private void Awake()
    {
        mainInventory = new InventorySlot[mainSlots];
        pockets = new InventorySlot[pocketSlots];

        for (int i = 0; i < mainSlots; i++)
            mainInventory[i] = new InventorySlot();
        for (int i = 0; i < pocketSlots; i++)
            pockets[i] = new InventorySlot();
    }

    public bool TryAddItem(ItemData item, int amount = 1)
    {
        // If stackable, try to add to existing stack
        if (item.isStackable)
        {
            // Check pockets first for materials
            if (item.isCraftingMaterial)
            {
                var slot = FindStackable(pockets, item);
                if (slot != null)
                {
                    slot.amount += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // Check main inventory
            var mainSlot = FindStackable(mainInventory, item);
            if (mainSlot != null)
            {
                mainSlot.amount += amount;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // Try empty pocket for materials
        if (item.isCraftingMaterial)
        {
            var emptyPocket = FindEmpty(pockets);
            if (emptyPocket != null)
            {
                emptyPocket.item = item;
                emptyPocket.amount = amount;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // Try empty main slot
        var emptyMain = FindEmpty(mainInventory);
        if (emptyMain != null)
        {
            emptyMain.item = item;
            emptyMain.amount = amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        Debug.Log("Инвентарь полон!");
        return false;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        int total = CountItem(item);
        return total >= amount;
    }

    public int CountItem(ItemData item)
    {
        int count = 0;

        foreach (var slot in mainInventory)
        {
            if (!slot.IsEmpty && slot.item == item)
                count += slot.amount;
        }

        foreach (var slot in pockets)
        {
            if (!slot.IsEmpty && slot.item == item)
                count += slot.amount;
        }

        return count;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        int remaining = amount;

        // Remove from pockets first
        remaining = RemoveFromSlots(pockets, item, remaining);

        // Then from main
        if (remaining > 0)
            remaining = RemoveFromSlots(mainInventory, item, remaining);

        if (remaining <= 0)
        {
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    private int RemoveFromSlots(
        InventorySlot[] slots, ItemData item, int amount)
    {
        int remaining = amount;

        foreach (var slot in slots)
        {
            if (slot.IsEmpty || slot.item != item)
                continue;

            if (slot.amount >= remaining)
            {
                slot.amount -= remaining;
                if (slot.amount <= 0) slot.Clear();
                return 0;
            }
            else
            {
                remaining -= slot.amount;
                slot.Clear();
            }
        }

        return remaining;
    }

    private InventorySlot FindStackable(
        InventorySlot[] slots, ItemData item)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty
                && slot.item == item
                && slot.amount < item.maxStack)
                return slot;
        }
        return null;
    }

    private InventorySlot FindEmpty(InventorySlot[] slots)
    {
        foreach (var slot in slots)
        {
            if (slot.IsEmpty) return slot;
        }
        return null;
    }
}