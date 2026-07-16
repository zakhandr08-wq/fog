using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerSanity : MonoBehaviour
{
    private FogBoundary fogBoundary;
    [Header("Settings")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float startSanity = 100f;

    [Header("Drain")]
    [SerializeField] private float darknessDrain = 3f;
    [SerializeField] private float lonelinessDrain = 2f;

    [Header("Detection")]
    [SerializeField] private float lightCheckRadius = 10f;
    [SerializeField] private float allyCheckRadius = 20f;

    [Header("Visual Effects")]
    [SerializeField] private Volume postProcessVolume;

    // State
    private float currentSanity;
    private bool isInLight;
    private bool isNearAlly;

    // Post-process effects
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private ColorAdjustments colorAdj;

    // Events
    public Action<float> OnSanityChanged;
    public float CurrentSanity => currentSanity;
    public float SanityNormalized => currentSanity / maxSanity;

    private void Awake()
    {
        currentSanity = startSanity;
    }

    private void Start()
    {
        fogBoundary = FindFirstObjectByType<FogBoundary>();
        // Get post-process effects
        if (postProcessVolume != null)
        {
            var profile = postProcessVolume.profile;
            profile.TryGet(out vignette);
            profile.TryGet(out chromatic);
            profile.TryGet(out colorAdj);
        }
    }

    private void Update()
    {
        CheckEnvironment();
        ApplyDrain();
        UpdateVisualEffects();
    }

    private void CheckEnvironment()
    {
        // Check if player is near light
        isInLight = false;
        var lights = FindObjectsByType<Light>(
            FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional) continue;

            float dist = Vector3.Distance(
                transform.position, light.transform.position);
            if (dist < light.range)
            {
                isInLight = true;
                break;
            }
        }

        // Check if near ally (for future multiplayer)
        // For now, always false in singleplayer
        isNearAlly = false;
    }

    private void ApplyDrain()
    {
        float drain = 0f;

        // Darkness drain
        if (!isInLight)
        {
            drain += darknessDrain;
        }

        // Apply drain per 10 seconds
        // (values in GDD are per 10 sec)
        float drainPerSecond = drain / 10f;
        currentSanity -= drainPerSecond * Time.deltaTime;
        currentSanity = Mathf.Clamp(
            currentSanity, 0f, maxSanity);

        OnSanityChanged?.Invoke(currentSanity);
    }

    public void RestoreSanity(float amount)
    {
        // amount is per 10 seconds in GDD
        float perSecond = amount / 10f;
        currentSanity += perSecond;
        currentSanity = Mathf.Clamp(
            currentSanity, 0f, maxSanity);

        OnSanityChanged?.Invoke(currentSanity);
    }

    public void DrainSanity(float amount)
    {
        currentSanity -= amount;
        currentSanity = Mathf.Clamp(
            currentSanity, 0f, maxSanity);

        OnSanityChanged?.Invoke(currentSanity);
    }

    private void UpdateVisualEffects()
    {

        if (postProcessVolume == null) return;

        float sanityPercent = SanityNormalized;

        // Vignette — increases as sanity drops
        if (vignette != null)
        {
            // 100% sanity = 0.25, 0% sanity = 0.6
            vignette.intensity.value =
                Mathf.Lerp(0.6f, 0.25f, sanityPercent);
        }

        // Chromatic Aberration — appears below 50%
        if (chromatic != null)
        {
            if (sanityPercent < 0.5f)
            {
                float t = 1f - (sanityPercent / 0.5f);
                chromatic.intensity.value =
                    Mathf.Lerp(0f, 0.8f, t);
            }
            else
            {
                chromatic.intensity.value = 0f;
            }
        }

        // Color desaturation below 25%
        if (colorAdj != null)
        {
            if (sanityPercent < 0.25f)
            {
                float t = 1f - (sanityPercent / 0.25f);
                colorAdj.saturation.value =
                    Mathf.Lerp(0f, -60f, t);
            }
            else
            {
                colorAdj.saturation.value = 0f;
            }
        }

    }

    // Get sanity tier for game logic
    public SanityTier GetSanityTier()
    {
        float percent = SanityNormalized * 100f;

        if (percent > 75f) return SanityTier.Calm;
        if (percent > 50f) return SanityTier.Anxiety;
        if (percent > 25f) return SanityTier.Fear;
        if (percent > 10f) return SanityTier.Panic;
        return SanityTier.Madness;
    }
}

public enum SanityTier
{
    Calm,      // 75-100%
    Anxiety,   // 50-75%
    Fear,      // 25-50%
    Panic,     // 10-25%
    Madness    // 0-10%
}
