using UnityEngine;

public abstract class EntityAbility : MonoBehaviour
{
    [Header("Ability Info")]
    [SerializeField] protected string abilityName;
    [SerializeField] protected string description;
    [SerializeField] protected Sprite icon;

    [Header("Cooldown")]
    [SerializeField] protected float cooldown = 5f;

    [Header("Targeting")]
    [SerializeField] protected float maxRange = 100f;
    [SerializeField] protected LayerMask targetMask = ~0;

    protected float cooldownTimer;

    public string AbilityName => abilityName;
    public string Description => description;
    public Sprite Icon => icon;

    public float Cooldown => cooldown;
    public float CooldownTimer => cooldownTimer;
    public bool IsReady => cooldownTimer <= 0f;

    public float CooldownNormalized
    {
        get
        {
            if (cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownTimer / cooldown);
        }
    }

    protected virtual void Start()
    {
        // Базовый Start нужен, чтобы дочерние способности могли делать base.Start()
    }

    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer < 0f)
                cooldownTimer = 0f;
        }
    }

    public virtual bool TryUse()
    {
        if (!IsReady)
        {
            Debug.Log($"{abilityName}: кулдаун {cooldownTimer:F1}с");
            return false;
        }

        if (!CanUse())
        {
            return false;
        }

        OnUse();
        cooldownTimer = cooldown;
        return true;
    }

    protected virtual bool CanUse()
    {
        return true;
    }

    protected abstract void OnUse();

    protected bool GetLookTarget(out RaycastHit hit)
    {
        hit = default;

        EntityController entity = GetComponentInParent<EntityController>();

        if (entity == null)
        {
            Debug.LogError($"{abilityName}: EntityController не найден в родителях!");
            return false;
        }

        Camera cam = entity.GetComponentInChildren<Camera>();

        if (cam == null)
        {
            Debug.LogError($"{abilityName}: камера Сущности не найдена!");
            return false;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return Physics.Raycast(ray, out hit, maxRange, targetMask);
    }
}