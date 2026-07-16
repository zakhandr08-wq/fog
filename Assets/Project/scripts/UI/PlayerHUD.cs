using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerSanity playerSanity;

    [Header("Stamina")]
    [SerializeField] private Image staminaBar;
    [SerializeField] private Color staminaFullColor = Color.yellow;
    [SerializeField] private Color staminaEmptyColor = Color.red;

    [Header("Sanity")]
    [SerializeField] private Image sanityBar;
    [SerializeField] private TextMeshProUGUI sanityText;
    [SerializeField] private Color sanityFullColor = Color.cyan;
    [SerializeField] private Color sanityLowColor = Color.red;

    private void Start()
    {
        if (playerController == null)
            playerController =
                FindFirstObjectByType<PlayerController>();

        if (playerSanity == null)
            playerSanity =
                FindFirstObjectByType<PlayerSanity>();
    }

    private void Update()
    {
        UpdateStamina();
        UpdateSanity();
    }

    private void UpdateStamina()
    {
        if (playerController == null) return;

        float normalized = playerController.StaminaNormalized;
        staminaBar.fillAmount = normalized;
        staminaBar.color = Color.Lerp(
            staminaEmptyColor, staminaFullColor, normalized);
    }

    private void UpdateSanity()
    {
        if (playerSanity == null) return;

        float normalized = playerSanity.SanityNormalized;
        sanityBar.fillAmount = normalized;
        sanityBar.color = Color.Lerp(
            sanityLowColor, sanityFullColor, normalized);

        if (sanityText != null)
        {
            int percent = Mathf.RoundToInt(normalized * 100f);
            sanityText.text = $"{percent}%";
        }
    }
}