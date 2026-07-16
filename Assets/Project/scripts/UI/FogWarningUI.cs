using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FogWarningUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image warningOverlay;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Settings")]
    [SerializeField] private float warnDistance = 15f;

    private FogBoundary fogBoundary;
    private PlayerController player;

    private void Start()
    {
        fogBoundary = FindFirstObjectByType<FogBoundary>();
        player = FindFirstObjectByType<PlayerController>();

        if (fogBoundary == null)
            Debug.LogError("FogWarningUI: FogBoundary not found!");
        if (player == null)
            Debug.LogError("FogWarningUI: Player not found!");

        // Убедимся что UI скрыт на старте
        HideAll();
    }

    private void Update()
    {
        if (player == null || fogBoundary == null) return;

        float dist = fogBoundary.GetDistanceToFog(
            player.transform.position);

        // Positive = safe, Negative = in fog
        bool nearFog = dist > 0f && dist < warnDistance;
        bool inFog = dist <= 0f;

        HandleWarningText(nearFog, inFog);
        HandleOverlay(inFog);
    }

    private void HandleWarningText(bool nearFog, bool inFog)
    {
        if (warningText == null) return;

        if (inFog)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "Туман поглощает тебя...";

            float pulse = (Mathf.Sin(
                Time.time * 2f) + 1f) / 2f;
            warningText.alpha =
                Mathf.Lerp(0.5f, 1f, pulse);
        }
        else if (nearFog)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "Дальше только туман...";
            warningText.alpha = 0.6f;
        }
        else
        {
            warningText.gameObject.SetActive(false);
        }
    }

    private void HandleOverlay(bool inFog)
    {
        if (warningOverlay == null) return;

        if (inFog && fogBoundary != null)
        {
            warningOverlay.gameObject.SetActive(true);
            var c = warningOverlay.color;
            c.a = fogBoundary.FogIntensity * 0.5f;
            warningOverlay.color = c;
        }
        else
        {
            warningOverlay.gameObject.SetActive(false);
        }
    }

    private void HideAll()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
        if (warningOverlay != null)
            warningOverlay.gameObject.SetActive(false);
    }
}