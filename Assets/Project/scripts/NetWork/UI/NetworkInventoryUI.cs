using UnityEngine;
using TMPro;

public class NetworkInventoryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI inventoryText;

    private NetworkPlayerInventory playerInventory;

    private void Update()
    {
        // »щем локальный инвентарь если ещЄ не нашли
        if (playerInventory == null)
        {
            var players =
                FindObjectsByType<NetworkPlayerInventory>(
                    FindObjectsSortMode.None);

            foreach (var p in players)
            {
                if (p.isLocalPlayer)
                {
                    playerInventory = p;
                    playerInventory.OnInventoryChanged += UpdateUI;
                    UpdateUI();
                    break;
                }
            }
            return;
        }

        // ѕосто€нно обновл€ем UI (простой вариант)
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (inventoryText == null || playerInventory == null)
            return;

        string text = "Inventory:\n";

        foreach (var item in playerInventory.Items)
        {
            text += $"Х {item.itemName} x{item.amount}\n";
        }

        if (playerInventory.Items.Count == 0)
            text += "(empty)";

        inventoryText.text = text;
    }
}