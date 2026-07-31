using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkSanityUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image sanityBar;
    [SerializeField] private TextMeshProUGUI sanityText;

    [Header("Colors")]
    [SerializeField] private Color fullColor = Color.cyan;
    [SerializeField] private Color lowColor = Color.red;

    private NetworkPlayerSanity localSanity;

    private void Update()
    {
        // »щем локальный Sanity если ещЄ не нашли
        if (localSanity == null)
        {
            var all = FindObjectsByType<NetworkPlayerSanity>(
                FindObjectsSortMode.None);

            foreach (var s in all)
            {
                if (s.isLocalPlayer)
                {
                    localSanity = s;
                    break;
                }
            }
            return;
        }

        UpdateBar();
    }

    private void UpdateBar()
    {
        float normalized = localSanity.SanityNormalized;

        if (sanityBar != null)
        {
            sanityBar.fillAmount = normalized;
            sanityBar.color = Color.Lerp(
                lowColor, fullColor, normalized);
        }

        if (sanityText != null)
        {
            int percent = Mathf.RoundToInt(normalized * 100f);
            sanityText.text = $"{percent}%";
        }
    }
}