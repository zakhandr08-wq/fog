using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeekerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SeekerController seeker;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI targetInfoText;
    [SerializeField] private Image attackCooldownImage;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 30f;

    private Camera seekerCamera;

    private void Start()
    {
        if (seeker == null)
            seeker = FindFirstObjectByType<SeekerController>();

        if (seeker != null)
            seekerCamera =
                seeker.GetComponentInChildren<Camera>(true);

        if (hintText != null)
        {
            hintText.text =
                "WASD Ч движение\n" +
                "Shift Ч бег\n" +
                "Space Ч прыжок\n" +
                "Ћ ћ Ч атака\n" +
                "F1 Ч ¬ыживший\n" +
                "F2 Ч —ущность\n" +
                "F3 Ч »щейка";
        }
    }

    private void OnEnable()
    {
        if (seeker != null)
            seekerCamera =
                seeker.GetComponentInChildren<Camera>(true);
    }

    private void Update()
    {
        UpdateCooldown();
        UpdateTargetInfo();
    }

    private void UpdateCooldown()
    {
        if (attackCooldownImage == null || seeker == null)
            return;

        float cd = seeker.GetAttackCooldownNormalized();

        if (cd > 0f)
        {
            attackCooldownImage.gameObject.SetActive(true);
            attackCooldownImage.fillAmount = cd;
        }
        else
        {
            attackCooldownImage.gameObject.SetActive(false);
        }
    }

    private void UpdateTargetInfo()
    {
        if (targetInfoText == null || seekerCamera == null)
            return;

        Ray ray = seekerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(
            ray, out RaycastHit hit, detectionRange))
        {
            var player = hit.collider
                .GetComponentInParent<PlayerHealth>();

            if (player != null)
            {
                targetInfoText.text =
                    "<color=red><b>∆≈–“¬ј</b></color>";
                return;
            }
        }

        targetInfoText.text = "";
    }
}
