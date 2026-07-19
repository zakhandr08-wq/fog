using UnityEngine;

public class CreakAbility : EntityAbility
{
    [Header("Creak Settings")]
    [SerializeField] private AudioClip[] creakSounds;
    [SerializeField] private float sanityDrain = 5f;
    [SerializeField] private float sanityRadius = 15f;
    [SerializeField] private float volume = 1f;
    [SerializeField] private GameObject creakVFXPrefab;

    protected override void Start()
    {
        base.Start();

        if (string.IsNullOrEmpty(abilityName))
            abilityName = "Скрип";
        if (string.IsNullOrEmpty(description))
            description = "Жуткий звук пугает выживших";
    }

    protected override bool CanUse()
    {
        if (!GetLookTarget(out RaycastHit hit))
        {
            Debug.Log("Скрип: не наведён на поверхность");
            return false;
        }

        return true;
    }

    protected override void OnUse()
    {
        if (!GetLookTarget(out RaycastHit hit)) return;

        Vector3 pos = hit.point;

        // Проиграть звук в точке
        if (creakSounds != null && creakSounds.Length > 0)
        {
            AudioClip clip = creakSounds[
                Random.Range(0, creakSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, pos, volume);
        }

        // Визуальный эффект
        if (creakVFXPrefab != null)
        {
            var vfx = Instantiate(
                creakVFXPrefab, pos, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Уменьшить рассудок игрокам в радиусе
        var colliders = Physics.OverlapSphere(
            pos, sanityRadius);

        foreach (var col in colliders)
        {
            var sanity = col.GetComponent<PlayerSanity>();
            if (sanity != null)
            {
                sanity.DrainSanity(sanityDrain);
                Debug.Log($"Скрип забрал {sanityDrain}% " +
                    $"рассудка у {col.name}");
            }
        }

        Debug.Log($"Скрип в точке {pos}");
    }
}