public enum PlayerState
{
    // Обычные
    Idle,
    Walking,
    Running,
    Jumping,

    // Раненые
    WoundedIdle,
    WoundedWalking,
    WoundedRunning,
    WoundedJumping,

    // Тяжело раненые
    HeavyWoundedIdle,
    HeavyWoundedWalking,
    HeavyWoundedRunning,
    HeavyWoundedJumping,

    // Особые (без движения)
    Downed,
    Dead
}