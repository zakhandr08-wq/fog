using UnityEngine;
using TMPro;

public class PlacementUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private TextMeshProUGUI placementHintText;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (placementSystem == null)
        {
            placementSystem =
                FindFirstObjectByType<PlacementSystem>();
        }

        if (placementHintText != null)
            placementHintText.gameObject.SetActive(false);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (placementSystem == null) return;

        bool placing = placementSystem.IsPlacing;

        // Show/hide hint
        if (placementHintText != null)
        {
            placementHintText.gameObject.SetActive(placing);

            if (placing)
            {
                placementHintText.text =
                    "Enter/Ћ ћ Ч поставить\n" +
                    "Esc/ѕ ћ Ч отмена\n" +
                    "Q/E Ч вращать";
            }
        }
    }
}