using UnityEngine;
using Mirror;
using System;

public class NetworkPlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxFalls = 3;

    // === СИНХРОНИЗИРУЕМЫЕ ПЕРЕМЕННЫЕ ===
    // hook — метод который вызывается когда переменная меняется
    [SyncVar(hook = nameof(OnWoundedChanged))]
    private bool syncIsWounded;

    [SyncVar(hook = nameof(OnHeavyWoundedChanged))]
    private bool syncIsHeavyWounded;

    [SyncVar(hook = nameof(OnDownedChanged))]
    private bool syncIsDowned;

    [SyncVar(hook = nameof(OnDeadChanged))]
    private bool syncIsDead;

    [SyncVar] private int syncCurrentFalls;

    // === PROPERTIES ===
    public bool IsWounded => syncIsWounded;
    public bool IsHeavyWounded => syncIsHeavyWounded;
    public bool IsDowned => syncIsDowned;
    public bool IsDead => syncIsDead;
    public int CurrentFalls => syncCurrentFalls;
    public int MaxFalls => maxFalls;

    // === EVENTS (локальные, для UI) ===
    public event Action OnWounded;
    public event Action OnHeavyWounded;
    public event Action OnDowned;
    public event Action OnRevived;
    public event Action OnDeath;

    // ====================================
    // SERVER-SIDE (только на сервере)
    // ====================================

    /// <summary>
    /// Вызывается на сервере когда игрок получает урон
    /// </summary>
    [Server]
    public void TakeDamage()
    {
        if (syncIsDead) return;

        if (syncIsDowned)
        {
            Kill();
            return;
        }

        if (!syncIsWounded)
        {
            syncIsWounded = true;
        }
        else if (!syncIsHeavyWounded)
        {
            syncIsHeavyWounded = true;
        }
        else
        {
            Down();
        }
    }

    [Server]
    public void Down()
    {
        if (syncIsDead) return;
        if (syncIsDowned) return;

        syncIsDowned = true;
        syncCurrentFalls++;

        if (syncCurrentFalls >= maxFalls)
        {
            Kill();
        }
    }

    [Server]
    public void Revive()
    {
        if (syncIsDead) return;
        if (!syncIsDowned) return;

        syncIsDowned = false;
        syncIsHeavyWounded = false;
        syncIsWounded = true;
    }

    [Server]
    public void Heal()
    {
        if (syncIsDead) return;
        if (syncIsDowned) return;

        syncIsWounded = false;
        syncIsHeavyWounded = false;
    }

    [Server]
    public void Kill()
    {
        if (syncIsDead) return;

        syncIsDead = true;
        syncIsDowned = true;
    }

    // ====================================
    // COMMAND (клиент → сервер)
    // ====================================

    /// <summary>
    /// Клиент запрашивает урон (для тестов)
    /// </summary>
    [Command]
    public void CmdTakeDamage()
    {
        TakeDamage();
    }

    [Command]
    public void CmdHeal()
    {
        Heal();
    }

    [Command]
    public void CmdRevive()
    {
        Revive();
    }

    // ====================================
    // HOOKS (вызываются при изменении SyncVar)
    // ====================================

    private void OnWoundedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            Debug.Log($"{name} ранен");
            OnWounded?.Invoke();
        }
    }

    private void OnHeavyWoundedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            Debug.Log($"{name} тяжело ранен");
            OnHeavyWounded?.Invoke();
        }
    }

    private void OnDownedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            Debug.Log($"{name} упал ({syncCurrentFalls}/{maxFalls})");
            OnDowned?.Invoke();
        }
        else if (!newValue && oldValue)
        {
            Debug.Log($"{name} поднят");
            OnRevived?.Invoke();
        }
    }

    private void OnDeadChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            Debug.Log($"{name} мёртв");
            OnDeath?.Invoke();
        }
    }

    // ====================================
    // ТЕСТОВЫЕ КЛАВИШИ
    // ====================================

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.T))
            CmdTakeDamage();

        if (Input.GetKeyDown(KeyCode.Y))
            CmdRevive();

        if (Input.GetKeyDown(KeyCode.U))
            CmdHeal();
    }
}