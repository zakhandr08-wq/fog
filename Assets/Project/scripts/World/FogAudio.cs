using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FogAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FogBoundary fogBoundary;

    [Header("Settings")]
    [SerializeField] private float maxVolume = 0.5f;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float startDistance = 20f;

    private AudioSource audioSource;
    private Transform playerTransform;
    private float targetVolume;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
        audioSource.Play();

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Calculate distance to nearest edge
        float distToEdge = GetDistanceToEdge();

        // Fade in as approaching edge
        if (distToEdge < startDistance)
        {
            float t = 1f - (distToEdge / startDistance);
            targetVolume = Mathf.Lerp(0f, maxVolume, t);
        }
        else
        {
            targetVolume = 0f;
        }

        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            targetVolume,
            Time.deltaTime * fadeSpeed
        );
    }

    private float GetDistanceToEdge()
    {
        if (fogBoundary == null) return 999f;

        Vector3 pos = playerTransform.position;
        float halfMap = 50f; // half of mapSize

        float distX = Mathf.Min(
            Mathf.Abs(pos.x - halfMap),
            Mathf.Abs(pos.x + halfMap)
        );

        float distZ = Mathf.Min(
            Mathf.Abs(pos.z - halfMap),
            Mathf.Abs(pos.z + halfMap)
        );

        return Mathf.Min(distX, distZ);
    }
}