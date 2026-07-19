using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntityAbilitiesUI : MonoBehaviour
{
    [System.Serializable]
    public class AbilitySlot
    {
        public Image iconImage;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        public TextMeshProUGUI keyText;
        public Image background;
    }

    [Header("References")]
    [SerializeField] private EntityAbilitiesManager manager;

    [Header("Slots")]
    [SerializeField] private AbilitySlot slotQ;
    [SerializeField] private AbilitySlot slotW;
    [SerializeField] private AbilitySlot slotE;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField]
    private Color cooldownColor =
        new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        if (manager == null)
            manager = FindFirstObjectByType<EntityAbilitiesManager>();

        SetupSlot(slotQ, manager.AbilityQ, "Q");
        SetupSlot(slotW, manager.AbilityW, "Y");
        SetupSlot(slotE, manager.AbilityE, "E");
    }

    private void SetupSlot(
        AbilitySlot slot, EntityAbility ability, string key)
    {
        if (slot == null) return;

        if (slot.keyText != null)
            slot.keyText.text = key;

        if (ability == null)
        {
            if (slot.iconImage != null)
                slot.iconImage.gameObject.SetActive(false);
            return;
        }

        if (slot.iconImage != null && ability.Icon != null)
            slot.iconImage.sprite = ability.Icon;
    }

    private void Update()
    {
        UpdateSlot(slotQ, manager.AbilityQ);
        UpdateSlot(slotW, manager.AbilityW);
        UpdateSlot(slotE, manager.AbilityE);
    }

    private void UpdateSlot(
        AbilitySlot slot, EntityAbility ability)
    {
        if (slot == null || ability == null) return;

        // Cooldown overlay
        if (slot.cooldownOverlay != null)
        {
            if (ability.CooldownTimer > 0f)
            {
                slot.cooldownOverlay.gameObject.SetActive(true);
                slot.cooldownOverlay.fillAmount =
                    ability.CooldownNormalized;
            }
            else
            {
                slot.cooldownOverlay.gameObject.SetActive(false);
            }
        }

        // Cooldown text
        if (slot.cooldownText != null)
        {
            if (ability.CooldownTimer > 0f)
            {
                slot.cooldownText.gameObject.SetActive(true);
                slot.cooldownText.text =
                    $"{ability.CooldownTimer:F1}";
            }
            else
            {
                slot.cooldownText.gameObject.SetActive(false);
            }
        }

        // Icon color
        if (slot.iconImage != null)
        {
            slot.iconImage.color = ability.IsReady
                ? readyColor
                : cooldownColor;
        }
    }
}