using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxFalls = 3;

    private int currentFalls;
    private bool isWounded;
    private bool isHeavyWounded;
    private bool isDowned;
    private bool isDead;

    public int CurrentFalls => currentFalls;
    public int MaxFalls => maxFalls;
    public bool IsWounded => isWounded;
    public bool IsHeavyWounded => isHeavyWounded;
    public bool IsDowned => isDowned;
    public bool IsDead => isDead;
    public bool IsHealthy =>
        !isWounded && !isHeavyWounded && !isDowned && !isDead;

    public Action OnWounded;
    public Action OnHeavyWounded;
    public Action OnDowned;
    public Action OnRevived;
    public Action OnDeath;

    /// <summary>
    /// Нанести урон — переход через все стадии
    /// </summary>
    public void TakeDamage()
    {
        if (isDead) return;

        // Если уже лежит — ещё один удар = смерть
        if (isDowned)
        {
            Kill();
            return;
        }

        if (!isWounded)
        {
            isWounded = true;
            OnWounded?.Invoke();
            Debug.Log("Игрок ранен");
        }
        else if (!isHeavyWounded)
        {
            isHeavyWounded = true;
            OnHeavyWounded?.Invoke();
            Debug.Log("Игрок тяжело ранен");
        }
        else
        {
            Down();
        }
    }

    /// <summary>
    /// Сбить с ног напрямую
    /// </summary>
    public void Down()
    {
        if (isDead) return;
        if (isDowned) return;

        isDowned = true;
        currentFalls++;

        OnDowned?.Invoke();
        Debug.Log($"Игрок упал ({currentFalls}/{maxFalls})");

        if (currentFalls >= maxFalls)
        {
            Kill();
        }
    }

    /// <summary>
    /// Поднять игрока
    /// После подъёма он в состоянии Wounded
    /// </summary>
    public void Revive()
    {
        if (isDead) return;
        if (!isDowned) return; // Не был упавшим

        isDowned = false;
        isHeavyWounded = false;
        isWounded = true; // ← всегда Wounded после подъёма

        OnRevived?.Invoke();
        Debug.Log("Игрок поднят (Wounded)");
    }

    /// <summary>
    /// Полное лечение
    /// </summary>
    public void Heal()
    {
        if (isDead) return;
        if (isDowned) return; // Нельзя лечить упавшего

        isWounded = false;
        isHeavyWounded = false;

        Debug.Log("Игрок полностью исцелён");
    }

    /// <summary>
    /// Убить
    /// </summary>
    public void Kill()
    {
        if (isDead) return;

        isDead = true;
        isDowned = true;

        OnDeath?.Invoke();
        Debug.Log("Игрок мёртв");
    }

    /// <summary>
    /// Полный сброс (для команды из консоли)
    /// </summary>
    public void FullReset()
    {
        isWounded = false;
        isHeavyWounded = false;
        isDowned = false;
        isDead = false;
        currentFalls = 0;

        Debug.Log("Здоровье полностью восстановлено");
    }

    // === Тестовые клавиши (удалить позже) ===
    private void Update()
    {
        if (DebugConsole.IsOpen) return;

        if (Input.GetKeyDown(KeyCode.T)) TakeDamage();
        if (Input.GetKeyDown(KeyCode.Y)) Revive();
        if (Input.GetKeyDown(KeyCode.U)) Heal();
        if (Input.GetKeyDown(KeyCode.O)) FullReset();
    }
}