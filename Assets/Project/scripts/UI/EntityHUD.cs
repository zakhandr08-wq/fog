using UnityEngine;
using TMPro;

public class EntityHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityController entity;
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI targetNameText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Target Detection")]
    [SerializeField] private float detectionRange = 100f;
    [SerializeField] private LayerMask targetMask = ~0;

    private Camera entityCamera;

    private void Start()
    {
        if (entity == null)
            entity = FindFirstObjectByType<EntityController>();

        if (entity != null)
            entityCamera = entity.GetComponentInChildren<Camera>(true);

        if (hintText != null)
        {
            hintText.text =
                "WASD Ч движение\n" +
                "ћышь Ч обзор\n" +
                "Space / C Ч вверх / вниз\n" +
                "Shift Ч быстро\n" +
                "Ctrl Ч медленно\n" +
                "ѕ ћ Ч приблизить\n" +
                "Q W E Ч способности\n" +
                "F1 Ч вернутьс€ к игроку";
        }

        if (targetNameText != null)
            targetNameText.text = "";
    }

    private void OnEnable()
    {
        // ќбновл€ем ссылку на камеру при активации HUD
        if (entity == null)
            entity = FindFirstObjectByType<EntityController>();

        if (entity != null)
            entityCamera = entity.GetComponentInChildren<Camera>(true);
    }

    private void Update()
    {
        UpdateTargetInfo();
        UpdatePositionInfo();
    }

    private void UpdateTargetInfo()
    {
        if (targetNameText == null || entityCamera == null)
        {
            if (targetNameText != null)
                targetNameText.text = "";
            return;
        }

        // Ћуч из центра экрана
        Ray ray = entityCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(
            ray, out RaycastHit hit,
            detectionRange, targetMask))
        {
            string info = GetTargetInfo(hit);
            targetNameText.text = info;
        }
        else
        {
            targetNameText.text = "";
        }
    }

    private string GetTargetInfo(RaycastHit hit)
    {
        // »грок
        if (hit.collider.GetComponentInParent<PlayerController>() != null)
        {
            return "<color=#FF3B3B><b>¬џ∆»¬Ў»…</b></color>";
        }

        //  остЄр
        if (hit.collider.GetComponentInParent<Bonfire>() != null)
        {
            return "<color=#FFA030><b> ќ—“®–</b></color>";
        }

        // –есурс (палки, камни)
        if (hit.collider.GetComponentInParent<PickupItem>() != null)
        {
            return "<color=#B0B0B0>–есурс</color>";
        }

        // ƒерево (если есть отдельный компонент)
        // if (hit.collider.GetComponentInParent<Tree>() != null)
        //     return "<color=#5A8F3A>ƒерево</color>";

        // Ќичего интересного
        return "";
    }

    private void UpdatePositionInfo()
    {
        if (positionText == null || entity == null) return;

        Vector3 pos = entity.transform.position;
        positionText.text =
            $"X: {pos.x:F0}  Y: {pos.y:F0}  Z: {pos.z:F0}";
    }
}