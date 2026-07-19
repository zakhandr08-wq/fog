using UnityEngine;

public class MarkAbility : EntityAbility
{
    [Header("Mark Settings")]
    [SerializeField] private float markDuration = 8f;
    [SerializeField] private GameObject markVFXPrefab;

    protected override void Start()
    {
        base.Start();

        if (string.IsNullOrEmpty(abilityName))
            abilityName = "Метка";
        if (string.IsNullOrEmpty(description))
            description = "Пометить выжившего на 8 сек";
    }

    protected override bool CanUse()
    {
        if (!GetLookTarget(out RaycastHit hit))
        {
            Debug.Log("Метка: не наведён на цель");
            return false;
        }

        Debug.Log($"Наведён на: {hit.collider.name}");

        // Ищем компонент во всём объекте включая родителей
        var player = hit.collider.GetComponent<PlayerController>();
        if (player == null)
            player = hit.collider.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            Debug.Log($"Цель {hit.collider.name} " +
                $"не является игроком");
            return false;
        }

        Debug.Log("Игрок найден!");
        return true;
    }

    protected override void OnUse()
    {
        if (!GetLookTarget(out RaycastHit hit)) return;

        var player = hit.collider.GetComponent<PlayerController>();
        if (player == null)
            player = hit.collider.GetComponentInParent<PlayerController>();

        if (player == null) return;

        var marker = player.GetComponent<PlayerMarker>();
        if (marker == null)
            marker = player.gameObject.AddComponent<PlayerMarker>();

        marker.Mark(markDuration, markVFXPrefab);

        Debug.Log($"Метка поставлена на {player.name}");
    }
}
