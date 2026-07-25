using UnityEngine;

public class SeekerAnimationEvents : MonoBehaviour
{
    private SeekerController seeker;

    private void Awake()
    {
        // Ищем SeekerController в родителе
        seeker = GetComponentInParent<SeekerController>();

        if (seeker == null)
            Debug.LogError("[SeekerAnimationEvents] " +
                "SeekerController not found in parent!");
    }

    /// <summary>
    /// Вызывается из Animation Event
    /// </summary>
    public void OnAttackHit()
    {
        if (seeker != null)
            seeker.OnAttackHit();
    }

    /// <summary>
    /// Вызывается из Animation Event (звук шага)
    /// </summary>
    public void OnFootstep()
    {
        // Пока пусто, потом добавим звук
        // Debug.Log("Footstep");
    }

    /// <summary>
    /// Начало атаки (замах)
    /// </summary>
    public void OnAttackStart()
    {
        // Можно добавить эффекты
    }

    /// <summary>
    /// Конец атаки
    /// </summary>
    public void OnAttackEnd()
    {
        // Можно сбросить что-то
    }
}