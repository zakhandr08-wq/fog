using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkingClip;

    [Header("Speed Threshold")]
    [SerializeField] private float minSpeed = 0.5f;

    [Header("Pitch (скорость воспроизведения)")]
    [SerializeField] private float walkPitch = 1f;
    [SerializeField] private float runPitch = 1.4f;
    [SerializeField] private float pitchSmoothing = 8f;

    [Header("Volume")]
    [SerializeField] private float walkVolume = 0.4f;
    [SerializeField] private float runVolume = 0.6f;
    [SerializeField] private float volumeSmoothing = 6f;
    [SerializeField] private float fadeOutSpeed = 10f;

    private PlayerController player;
    private CharacterController charController;
    private float targetPitch;
    private float targetVolume;
    private bool isPlaying;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        charController = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        audioSource.clip = walkingClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.pitch = walkPitch;
    }

    private void Update()
    {
        if (charController == null) return;

        Vector3 vel = charController.velocity;
        vel.y = 0f;
        float speed = vel.magnitude;

        bool shouldPlay = charController.isGrounded
            && speed > minSpeed;

        if (shouldPlay)
        {
            // Определяем pitch и volume
            bool isRunning = player != null
                && player.IsSprinting;

            targetPitch = isRunning ? runPitch : walkPitch;
            targetVolume = isRunning ? runVolume : walkVolume;

            // Запускаем если ещё не играет
            if (!audioSource.isPlaying)
                audioSource.Play();

            isPlaying = true;
        }
        else
        {
            // Затухание
            targetVolume = 0f;

            if (audioSource.volume < 0.01f && isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
            }
        }

        // Плавное изменение pitch и volume
        audioSource.pitch = Mathf.Lerp(
            audioSource.pitch,
            targetPitch,
            Time.deltaTime * pitchSmoothing);

        float smoothing = targetVolume > 0f
            ? volumeSmoothing
            : fadeOutSpeed;

        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            targetVolume,
            Time.deltaTime * smoothing);
    }
}