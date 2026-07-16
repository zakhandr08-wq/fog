using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerHandSystem handSystem;

    [Header("Panel")]
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private float slideSpeed = 8f;

    [Header("Main Slots (4)")]
    [SerializeField] private TextMeshProUGUI mainSlot1;
    [SerializeField] private TextMeshProUGUI mainSlot2;
    [SerializeField] private TextMeshProUGUI mainSlot3;
    [SerializeField] private TextMeshProUGUI mainSlot4;

    [Header("Pocket Slots (2)")]
    [SerializeField] private TextMeshProUGUI pocketSlot1;
    [SerializeField] private TextMeshProUGUI pocketSlot2;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.yellow;

    private TextMeshProUGUI[] mainSlots;
    private TextMeshProUGUI[] pocketSlots;

    private bool isOpen;
    private float hiddenY;
    private float shownY;

    private void Awake()
    {
        mainSlots = new TextMeshProUGUI[]
        {
            mainSlot1, mainSlot2, mainSlot3, mainSlot4
        };

        pocketSlots = new TextMeshProUGUI[]
        {
            pocketSlot1, pocketSlot2
        };

        shownY = inventoryPanel.anchoredPosition.y;
        hiddenY = shownY - 120f;

        var pos = inventoryPanel.anchoredPosition;
        pos.y = hiddenY;
        inventoryPanel.anchoredPosition = pos;
        isOpen = false;
    }

    private void OnEnable()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= UpdateUI;
    }

    private void Start()
    {
        if (playerInventory == null)
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        if (handSystem == null)
            handSystem =
                FindFirstObjectByType<PlayerHandSystem>();

        if (playerInventory != null)
            playerInventory.OnInventoryChanged += UpdateUI;

        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isOpen = !isOpen;
        }

        AnimatePanel();
        UpdateUI();
    }

    private void AnimatePanel()
    {
        float targetY = isOpen ? shownY : hiddenY;
        var pos = inventoryPanel.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, targetY,
            Time.deltaTime * slideSpeed);
        inventoryPanel.anchoredPosition = pos;
    }

    private void UpdateUI()
    {
        if (playerInventory == null) return;

        int selected = handSystem != null
            ? handSystem.SelectedSlot : -1;

        for (int i = 0; i < mainSlots.Length; i++)
        {
            var slot = playerInventory.MainInventory[i];

            if (!slot.IsEmpty)
            {
                string text = slot.item.itemName;
                if (slot.amount > 1)
                    text += $" x{slot.amount}";

                mainSlots[i].text = $"[{i + 1}: {text}]";

                // ѕодсветка выбранного
                mainSlots[i].color = (i == selected)
                    ? selectedColor
                    : normalColor;
            }
            else
            {
                mainSlots[i].text = $"[{i + 1}: Ч]";
                mainSlots[i].color = emptyColor;
            }
        }

        for (int i = 0; i < pocketSlots.Length; i++)
        {
            var slot = playerInventory.Pockets[i];

            if (!slot.IsEmpty)
            {
                pocketSlots[i].text =
                    $"[{slot.item.itemName} x{slot.amount}]";
                pocketSlots[i].color =
                    new Color(0.8f, 0.9f, 0.8f);
            }
            else
            {
                pocketSlots[i].text = "[Ч]";
                pocketSlots[i].color = emptyColor;
            }
        }
    }

    public bool IsOpen => isOpen;
}