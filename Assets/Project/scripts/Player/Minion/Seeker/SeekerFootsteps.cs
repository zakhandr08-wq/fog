using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SeekerFootsteps : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;

    [Header("Settings")]
    [SerializeField] private float minSpeed = 0.5f;

    [Header("Volume")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.8f;
    [SerializeField] private float volumeSmoothing = 6f;
    [SerializeField] private float fadeOutSpeed = 10f;

    [Header("Pitch")]
    [SerializeField] private float walkPitch = 0.95f;
    [SerializeField] private float runPitch = 1.1f;
    [SerializeField] private float pitchSmoothing = 8f;

    private SeekerController seeker;
    private CharacterController charController;
    private float targetVolume;
    private float targetPitch;
    private AudioClip currentClip;

    private void Awake()
    {
        seeker = GetComponent<SeekerController>();
        charController = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 40f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
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
            bool isRunning = seeker != null
                && seeker.IsRunning;

            AudioClip desiredClip = isRunning
                ? runClip
                : walkClip;

            targetVolume = isRunning ? runVolume : walkVolume;
            targetPitch = isRunning ? runPitch : walkPitch;

            // Переключаем клип если нужно
            if (currentClip != desiredClip && desiredClip != null)
            {
                currentClip = desiredClip;
                audioSource.clip = currentClip;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying && currentClip != null)
            {
                audioSource.Play();
            }
        }
        else
        {
            targetVolume = 0f;

            if (audioSource.volume < 0.01f
                && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        // Smoothing
        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            targetVolume,
            Time.deltaTime * (targetVolume > 0f
                ? volumeSmoothing
                : fadeOutSpeed));

        audioSource.pitch = Mathf.Lerp(
            audioSource.pitch,
            targetPitch,
            Time.deltaTime * pitchSmoothing);
    }
}