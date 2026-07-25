using System;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerHealth health;

    [Header("Settings")]
    [SerializeField] private float movementThreshold = 0.1f;

    private PlayerState currentState = PlayerState.Idle;
    private CharacterController charController;

    public Action<PlayerState> OnStateChanged;
    public PlayerState CurrentState => currentState;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (health == null)
            health = GetComponent<PlayerHealth>();

        charController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        PlayerState newState = DetermineState();

        if (newState != currentState)
            SetState(newState);
    }

    private PlayerState DetermineState()
    {
        // Смерть и падение — абсолютный приоритет
        if (health != null)
        {
            if (health.IsDead) return PlayerState.Dead;
            if (health.IsDowned) return PlayerState.Downed;
        }

        // Определяем движение
        MovementType movement = GetMovementType();

        // Определяем уровень ранения
        WoundLevel wound = GetWoundLevel();

        // Комбинируем
        return CombineState(wound, movement);
    }

    private enum MovementType
    {
        Idle,
        Walking,
        Running,
        Jumping
    }

    private enum WoundLevel
    {
        Healthy,
        Wounded,
        HeavyWounded
    }

    private MovementType GetMovementType()
    {
        if (controller == null) return MovementType.Idle;

        if (!controller.IsGrounded)
            return MovementType.Jumping;

        if (controller.IsSprinting)
            return MovementType.Running;

        float speed = 0f;
        if (charController != null)
        {
            Vector3 horizontal = new Vector3(
                charController.velocity.x, 0f,
                charController.velocity.z);
            speed = horizontal.magnitude;
        }

        if (speed > movementThreshold)
            return MovementType.Walking;

        return MovementType.Idle;
    }

    private WoundLevel GetWoundLevel()
    {
        if (health == null) return WoundLevel.Healthy;
        if (health.IsHeavyWounded) return WoundLevel.HeavyWounded;
        if (health.IsWounded) return WoundLevel.Wounded;
        return WoundLevel.Healthy;
    }

    private PlayerState CombineState(
        WoundLevel wound, MovementType movement)
    {
        switch (wound)
        {
            case WoundLevel.Healthy:
                return movement switch
                {
                    MovementType.Idle => PlayerState.Idle,
                    MovementType.Walking => PlayerState.Walking,
                    MovementType.Running => PlayerState.Running,
                    MovementType.Jumping => PlayerState.Jumping,
                    _ => PlayerState.Idle,
                };

            case WoundLevel.Wounded:
                return movement switch
                {
                    MovementType.Idle => PlayerState.WoundedIdle,
                    MovementType.Walking => PlayerState.WoundedWalking,
                    MovementType.Running => PlayerState.WoundedRunning,
                    MovementType.Jumping => PlayerState.WoundedJumping,
                    _ => PlayerState.WoundedIdle,
                };

            case WoundLevel.HeavyWounded:
                return movement switch
                {
                    MovementType.Idle => PlayerState.HeavyWoundedIdle,
                    MovementType.Walking => PlayerState.HeavyWoundedWalking,
                    MovementType.Running => PlayerState.HeavyWoundedRunning,
                    MovementType.Jumping => PlayerState.HeavyWoundedJumping,
                    _ => PlayerState.HeavyWoundedIdle,
                };
        }

        return PlayerState.Idle;
    }

    private void SetState(PlayerState newState)
    {
        PlayerState oldState = currentState;
        currentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"State: {oldState} → {newState}");
    }
}
