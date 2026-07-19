using UnityEngine;

public class ExtinguishAbility : EntityAbility
{
    [Header("Extinguish Settings")]
    [SerializeField] private GameObject extinguishVFXPrefab;

    protected override void Start()
    {
        base.Start();

        if (string.IsNullOrEmpty(abilityName))
            abilityName = "Погасить";
        if (string.IsNullOrEmpty(description))
            description = "Потушить костёр";
    }

    protected override bool CanUse()
    {
        if (!GetLookTarget(out RaycastHit hit))
        {
            Debug.Log("Погасить: не наведён на цель");
            return false;
        }

        var bonfire = hit.collider
            .GetComponentInParent<Bonfire>();
        if (bonfire == null)
        {
            Debug.Log("Погасить: цель не костёр");
            return false;
        }

        return true;
    }

    protected override void OnUse()
    {
        if (!GetLookTarget(out RaycastHit hit)) return;

        var bonfire = hit.collider
            .GetComponentInParent<Bonfire>();

        if (bonfire == null) return;

        Vector3 pos = bonfire.transform.position;

        // VFX
        if (extinguishVFXPrefab != null)
        {
            var vfx = Instantiate(
                extinguishVFXPrefab,
                pos + Vector3.up * 0.5f,
                Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Уничтожить костёр
        Destroy(bonfire.gameObject);

        Debug.Log("Костёр потушен");
    }
}