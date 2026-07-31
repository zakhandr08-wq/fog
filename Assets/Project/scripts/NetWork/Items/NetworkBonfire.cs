using UnityEngine;
using Mirror;

public class NetworkBonfire : NetworkBehaviour
{
    [Header("Light")]
    [SerializeField] private float lightRadius = 10f;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField]
    private Color lightColor =
        new Color(1f, 0.6f, 0.2f);

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float flickerAmount = 0.3f;

    [Header("Sanity")]
    [SerializeField] private float sanityRestoreRadius = 8f;
    [SerializeField] private float sanityRestoreRate = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireLoopClip;
    [SerializeField] private float fireVolume = 0.6f;
    [SerializeField] private float fireMaxDistance = 15f;

    [SyncVar(hook = nameof(OnLitChanged))]
    private bool syncIsLit = true;

    private Light fireLight;
    private AudioSource fireAudio;
    private ParticleSystem[] particles;
    private float baseIntensity;
    private float lastSanityCheckTime;

    public bool IsLit => syncIsLit;

    private void Start()
    {
        SetupLight();
        SetupParticles();
        SetupAudio();
    }

    private void SetupLight()
    {
        Transform firePoint = transform.Find("FirePoint");

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = Vector3.up * 0.5f;
            firePoint = fp.transform;
        }

        fireLight = GetComponentInChildren<Light>();

        if (fireLight == null)
        {
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(firePoint);
            lightObj.transform.localPosition = Vector3.zero;
            fireLight = lightObj.AddComponent<Light>();
        }

        fireLight.type = LightType.Point;
        fireLight.range = lightRadius;
        fireLight.intensity = lightIntensity;
        fireLight.color = lightColor;
        fireLight.shadows = LightShadows.Soft;

        baseIntensity = lightIntensity;
    }

    private void SetupParticles()
    {
        Transform firePoint = transform.Find("FirePoint");
        particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in particles)
        {
            ps.gameObject.SetActive(true);

            if (firePoint != null && ps.transform.parent != firePoint)
            {
                ps.transform.SetParent(firePoint);
                ps.transform.localPosition = Vector3.zero;
            }

            if (!ps.isPlaying)
            {
                ps.Clear();
                ps.Play();
            }
        }
    }

    private void SetupAudio()
    {
        if (fireLoopClip == null) return;

        Transform firePoint = transform.Find("FirePoint");
        Transform audioParent = firePoint != null
            ? firePoint : transform;

        GameObject audioObj = new GameObject("FireAudio");
        audioObj.transform.SetParent(audioParent);
        audioObj.transform.localPosition = Vector3.zero;

        fireAudio = audioObj.AddComponent<AudioSource>();
        fireAudio.clip = fireLoopClip;
        fireAudio.loop = true;
        fireAudio.playOnAwake = true;
        fireAudio.volume = fireVolume;
        fireAudio.spatialBlend = 1f;
        fireAudio.maxDistance = fireMaxDistance;
        fireAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        fireAudio.Play();
    }

    private void Update()
    {
        UpdateFlicker();

        // Сервер восстанавливает рассудок ближним
        if (isServer && syncIsLit)
        {
            if (Time.time - lastSanityCheckTime >= 0.5f)
            {
                lastSanityCheckTime = Time.time;
                ServerRestoreSanityNearby();
            }
        }
    }

    private void UpdateFlicker()
    {
        if (fireLight == null || !syncIsLit) return;

        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed, 0f);
        fireLight.intensity = baseIntensity
            + (noise - 0.5f) * flickerAmount * baseIntensity;
    }

    [Server]
    private void ServerRestoreSanityNearby()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, sanityRestoreRadius);

        foreach (var col in colliders)
        {
            var sanity = col.GetComponent<NetworkPlayerSanity>();
            if (sanity != null)
            {
                // sanityRestoreRate — за 10 секунд
                float restore = sanityRestoreRate * 0.5f / 10f;
                sanity.ServerRestoreSanity(restore);
            }
        }
    }

    private void OnLitChanged(bool oldValue, bool newValue)
    {
        if (fireLight != null)
            fireLight.enabled = newValue;

        if (fireAudio != null)
        {
            if (newValue) fireAudio.Play();
            else fireAudio.Stop();
        }

        foreach (var ps in particles)
        {
            if (ps == null) continue;
            if (newValue) ps.Play();
            else ps.Stop();
        }
    }

    // ====================================
    // SERVER METHODS
    // ====================================

    [Server]
    public void ServerExtinguish()
    {
        syncIsLit = false;
    }

    [Server]
    public void ServerLight()
    {
        syncIsLit = true;
    }

    // Gizmos для отладки
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, lightRadius);

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, sanityRestoreRadius);
    }
}