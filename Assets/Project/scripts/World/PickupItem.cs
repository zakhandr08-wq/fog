using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    public string GetInteractionPrompt()
    {
        if (itemData == null)
            return "Предмет не настроен";

        if (amount > 1)
            return $"Подобрать {itemData.itemName} x{amount} [E]";

        return $"Подобрать {itemData.itemName} [E]";
    }

    public float GetInteractionTime()
    {
        return 0f;
    }

    public bool CanInteract()
    {
        return itemData != null;
    }

    public void OnInteractionStart()
    {
        Debug.Log("Interaction started");
    }

    public void OnInteractionComplete()
    {
        Debug.Log("=== OnInteractionComplete called ===");

        if (itemData == null)
        {
            Debug.LogError("ItemData is NULL!", this);
            return;
        }

        Debug.Log($"Item: {itemData.itemName}");

        var inventory = FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError("PlayerInventory NOT FOUND! " +
                "Добавь PlayerInventory на Player!", this);
            return;
        }

        Debug.Log("PlayerInventory found");

        bool added = inventory.TryAddItem(itemData, amount);

        Debug.Log($"TryAddItem result: {added}");

        if (added)
        {
            Debug.Log("Destroying pickup");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("Failed to add item - inventory full?");
        }
    }

    public void OnInteractionCancel()
    {
        Debug.Log("Interaction cancelled");
    }

    private void OnValidate()
    {
        if (amount < 1)
            amount = 1;
    }
}
