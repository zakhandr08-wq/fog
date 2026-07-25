using UnityEngine;
using UnityEngine.UI;

public class PlayerStateIcon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStateManager stateManager;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerSanity sanity;
    [SerializeField] private Image iconImage;

    [Header("Healthy Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite walkingSprite;
    [SerializeField] private Sprite runningSprite;
    [SerializeField] private Sprite jumpingSprite;

    [Header("Wounded Sprites")]
    [SerializeField] private Sprite woundedIdleSprite;
    [SerializeField] private Sprite woundedWalkingSprite;
    [SerializeField] private Sprite woundedRunningSprite;
    [SerializeField] private Sprite woundedJumpingSprite;

    [Header("Heavy Wounded Sprites")]
    [SerializeField] private Sprite heavyIdleSprite;
    [SerializeField] private Sprite heavyWalkingSprite;
    [SerializeField] private Sprite heavyRunningSprite;
    [SerializeField] private Sprite heavyJumpingSprite;

    [Header("Special Sprites")]
    [SerializeField] private Sprite downedSprite;
    [SerializeField] private Sprite deadSprite;

    [Header("Stamina Fading")]
    [SerializeField] private float minStaminaAlpha = 0.25f;
    [SerializeField] private float maxStaminaAlpha = 1f;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("State Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField]
    private Color woundedColor =
        new Color(1f, 0.7f, 0.5f, 1f);
    [SerializeField]
    private Color heavyColor =
        new Color(1f, 0.4f, 0.3f, 1f);
    [SerializeField]
    private Color downedColor =
        new Color(0.7f, 0.1f, 0.1f, 1f);
    [SerializeField]
    private Color deadColor =
        new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Pulse")]
    [SerializeField] private float pulseWounded = 1.5f;
    [SerializeField] private float pulseHeavy = 3f;
    [SerializeField] private float pulseDowned = 5f;

    [Header("Low Sanity")]
    [SerializeField] private float sanityThreshold = 0.3f;
    [SerializeField] private float sanityFlickerSpeed = 8f;

    private float currentAlpha = 1f;
    private Color targetColor;

    private void Start()
    {
        if (stateManager == null)
            stateManager = FindFirstObjectByType<PlayerStateManager>();
        if (controller == null)
            controller = FindFirstObjectByType<PlayerController>();
        if (sanity == null)
            sanity = FindFirstObjectByType<PlayerSanity>();

        if (stateManager != null)
        {
            stateManager.OnStateChanged += OnStateChanged;
            OnStateChanged(stateManager.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (stateManager != null)
            stateManager.OnStateChanged -= OnStateChanged;
    }

    private void Update()
    {
        UpdateAlpha();
        UpdateColor();
    }

    private void OnStateChanged(PlayerState state)
    {
        if (iconImage == null) return;

        Sprite sprite = GetSpriteForState(state);
        if (sprite != null)
            iconImage.sprite = sprite;

        targetColor = GetColorForState(state);
    }

    private Sprite GetSpriteForState(PlayerState state)
    {
        switch (state)
        {
            // Healthy
            case PlayerState.Idle: return idleSprite;
            case PlayerState.Walking: return walkingSprite;
            case PlayerState.Running: return runningSprite;
            case PlayerState.Jumping: return jumpingSprite;

            // Wounded
            case PlayerState.WoundedIdle: return woundedIdleSprite;
            case PlayerState.WoundedWalking: return woundedWalkingSprite;
            case PlayerState.WoundedRunning: return woundedRunningSprite;
            case PlayerState.WoundedJumping: return woundedJumpingSprite;

            // Heavy Wounded
            case PlayerState.HeavyWoundedIdle: return heavyIdleSprite;
            case PlayerState.HeavyWoundedWalking: return heavyWalkingSprite;
            case PlayerState.HeavyWoundedRunning: return heavyRunningSprite;
            case PlayerState.HeavyWoundedJumping: return heavyJumpingSprite;

            // Special
            case PlayerState.Downed: return downedSprite;
            case PlayerState.Dead: return deadSprite;

            default: return idleSprite;
        }
    }

    private Color GetColorForState(PlayerState state)
    {
        // Определяем "уровень" ранения по префиксу
        if (state == PlayerState.Dead) return deadColor;
        if (state == PlayerState.Downed) return downedColor;

        if (state.ToString().StartsWith("HeavyWounded"))
            return heavyColor;

        if (state.ToString().StartsWith("Wounded"))
            return woundedColor;

        return normalColor;
    }

    private void UpdateAlpha()
    {
        if (iconImage == null || stateManager == null) return;

        float targetAlpha = maxStaminaAlpha;

        // Выцветание при беге (все варианты running)
        bool isRunning =
            stateManager.CurrentState == PlayerState.Running
            || stateManager.CurrentState == PlayerState.WoundedRunning
            || stateManager.CurrentState == PlayerState.HeavyWoundedRunning;

        if (isRunning && controller != null)
        {
            float stamina = controller.StaminaNormalized;
            targetAlpha = Mathf.Lerp(
                minStaminaAlpha, maxStaminaAlpha, stamina);
        }

        // Пульсация при ранении
        float pulseSpeed = GetPulseSpeed();
        if (pulseSpeed > 0f)
        {
            float pulse = (Mathf.Sin(
                Time.time * pulseSpeed) + 1f) / 2f;
            targetAlpha *= Mathf.Lerp(0.5f, 1f, pulse);
        }

        // Мерцание при низком рассудке
        if (sanity != null
            && sanity.SanityNormalized < sanityThreshold)
        {
            float flickerT = sanity.SanityNormalized
                / sanityThreshold;
            float flicker = Mathf.PerlinNoise(
                Time.time * sanityFlickerSpeed, 0f);
            targetAlpha *= Mathf.Lerp(flicker, 1f, flickerT);
        }

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha,
            Time.deltaTime * fadeSpeed);

        Color c = iconImage.color;
        c.a = currentAlpha;
        iconImage.color = c;
    }

    private void UpdateColor()
    {
        if (iconImage == null) return;

        Color c = iconImage.color;
        c.r = Mathf.Lerp(c.r, targetColor.r,
            Time.deltaTime * fadeSpeed);
        c.g = Mathf.Lerp(c.g, targetColor.g,
            Time.deltaTime * fadeSpeed);
        c.b = Mathf.Lerp(c.b, targetColor.b,
            Time.deltaTime * fadeSpeed);
        iconImage.color = c;
    }

    private float GetPulseSpeed()
    {
        if (stateManager == null) return 0f;

        PlayerState s = stateManager.CurrentState;

        if (s == PlayerState.Downed) return pulseDowned;
        if (s == PlayerState.Dead) return 0f;

        if (s.ToString().StartsWith("HeavyWounded"))
            return pulseHeavy;
        if (s.ToString().StartsWith("Wounded"))
            return pulseWounded;

        return 0f;
    }
}