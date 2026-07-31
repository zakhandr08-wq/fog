using UnityEngine;
using Mirror;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class NetworkPlayerSanity : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float startSanity = 100f;

    [Header("Drain")]
    [SerializeField] private float darknessDrain = 3f;
    [SerializeField] private float lonelinessDrain = 2f;

    [Header("Detection")]
    [SerializeField] private float lightCheckRadius = 10f;

    [Header("Visual Effects")]
    [SerializeField] private Volume postProcessVolume;

    // === Синхронизируемое ===
    [SyncVar(hook = nameof(OnSanityChanged))]
    private float syncCurrentSanity;

    // === Локальное — эффекты и таймеры ===
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private ColorAdjustments colorAdj;

    private bool isInLight;
    private float lastCheckTime;

    // === Properties ===
    public float CurrentSanity => syncCurrentSanity;
    public float SanityNormalized => syncCurrentSanity / maxSanity;
    public float MaxSanity => maxSanity;

    // Events
    public event Action<float> OnSanityUpdated;

    public override void OnStartServer()
    {
        base.OnStartServer();
        syncCurrentSanity = startSanity;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Ищем post-processing volume в сцене если не назначен
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        if (postProcessVolume != null)
        {
            var profile = postProcessVolume.profile;
            profile.TryGet(out vignette);
            profile.TryGet(out chromatic);
            profile.TryGet(out colorAdj);
        }
    }

    // ====================================
    // SERVER METHODS
    // ====================================

    [Server]
    public void ServerDrainSanity(float amount)
    {
        syncCurrentSanity = Mathf.Max(0f, syncCurrentSanity - amount);
    }

    [Server]
    public void ServerRestoreSanity(float amount)
    {
        syncCurrentSanity = Mathf.Min(maxSanity, syncCurrentSanity + amount);
    }

    [Server]
    public void ServerSetSanity(float value)
    {
        syncCurrentSanity = Mathf.Clamp(value, 0f, maxSanity);
    }

    // ====================================
    // SERVER UPDATE - drain от окружения
    // ====================================

    private void Update()
    {
        // Только сервер проверяет окружение и снижает рассудок
        if (!isServer) return;

        // Проверяем окружение раз в 0.5 сек чтобы не грузить каждый кадр
        if (Time.time - lastCheckTime < 0.5f) return;
        lastCheckTime = Time.time;

        ServerCheckEnvironment();
    }

    [Server]
    private void ServerCheckEnvironment()
    {
        // Проверка: есть ли рядом источник света
        bool inLight = IsNearLight();

        // Если не в свете — снижаем рассудок
        if (!inLight)
        {
            // darknessDrain — за 10 секунд, поэтому за 0.5 сек:
            float drain = darknessDrain * 0.5f / 10f;
            ServerDrainSanity(drain);
        }
    }

    [Server]
    private bool IsNearLight()
    {
        // Ищем источники света в радиусе
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, lightCheckRadius);

        foreach (var col in colliders)
        {
            // Проверяем есть ли на объекте компонент Light
            var light = col.GetComponentInChildren<Light>();
            if (light != null && light.enabled && light.type != LightType.Directional)
            {
                // Проверяем дистанцию
                float dist = Vector3.Distance(
                    transform.position, light.transform.position);
                if (dist <= light.range)
                    return true;
            }
        }

        return false;
    }

    // ====================================
    // CLIENT — визуальные эффекты
    // ====================================

    private void OnSanityChanged(float oldValue, float newValue)
    {
        OnSanityUpdated?.Invoke(newValue);

        // Обновляем визуальные эффекты только у своего игрока
        if (isLocalPlayer)
        {
            UpdateVisualEffects();
        }
    }

    private void UpdateVisualEffects()
    {
        if (postProcessVolume == null) return;

        float sanityPercent = SanityNormalized;

        // Vignette
        if (vignette != null)
        {
            vignette.intensity.value =
                Mathf.Lerp(0.6f, 0.25f, sanityPercent);
        }

        // Chromatic Aberration
        if (chromatic != null)
        {
            if (sanityPercent < 0.5f)
            {
                float t = 1f - (sanityPercent / 0.5f);
                chromatic.intensity.value = Mathf.Lerp(0f, 0.8f, t);
            }
            else
            {
                chromatic.intensity.value = 0f;
            }
        }

        // Color desaturation
        if (colorAdj != null)
        {
            if (sanityPercent < 0.25f)
            {
                float t = 1f - (sanityPercent / 0.25f);
                colorAdj.saturation.value = Mathf.Lerp(0f, -60f, t);
            }
            else
            {
                colorAdj.saturation.value = 0f;
            }
        }
    }

    // ====================================
    // ТЕСТ КЛАВИШИ
    // ====================================

    private void HandleTestKeys()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            CmdTestDrain(20f);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            CmdTestRestore(20f);
        }
    }

    [Command]
    private void CmdTestDrain(float amount)
    {
        ServerDrainSanity(amount);
    }

    [Command]
    private void CmdTestRestore(float amount)
    {
        ServerRestoreSanity(amount);
    }

    private void LateUpdate()
    {
        HandleTestKeys();
    }
}