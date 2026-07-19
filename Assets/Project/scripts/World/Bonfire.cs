using UnityEngine;

public class Bonfire : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private float lightRadius = 10f;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField]
    private Color lightColor =
        new Color(1f, 0.6f, 0.2f);

    [Header("Sanity")]
    [SerializeField] private float sanityRestoreRadius = 8f;
    [SerializeField] private float sanityRestoreRate = 5f;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float flickerAmount = 0.3f;

    private Light fireLight;
    private float baseIntensity;
    private ParticleSystem[] particles;

    private void Start()
    {
        SetupLight();
        SetupParticles();
    }

    private void SetupLight()
    {
        // Ищем FirePoint
        Transform firePoint = transform.Find("FirePoint");

        if (firePoint == null)
        {
            // Если нет FirePoint — создаём
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = Vector3.up * 0.5f;
            firePoint = fp.transform;
            Debug.LogWarning("Bonfire: FirePoint not found, created at Y=0.5");
        }

        // Ищем существующий свет
        fireLight = GetComponentInChildren<Light>();

        if (fireLight == null)
        {
            // Создаём новый свет
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(firePoint);
            lightObj.transform.localPosition = Vector3.zero;
            fireLight = lightObj.AddComponent<Light>();
        }
        else
        {
            // Перемещаем существующий свет в FirePoint
            fireLight.transform.SetParent(firePoint);
            fireLight.transform.localPosition = Vector3.zero;
        }

        // Настройка
        fireLight.type = LightType.Point;
        fireLight.range = lightRadius;
        fireLight.intensity = lightIntensity;
        fireLight.color = lightColor;
        fireLight.shadows = LightShadows.Soft;

        baseIntensity = lightIntensity;
    }

    private void SetupParticles()
    {
        // Ищем FirePoint для партиклов тоже
        Transform firePoint = transform.Find("FirePoint");

        particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in particles)
        {
            // Включаем если был выключен
            ps.gameObject.SetActive(true);

            // Перемещаем к FirePoint если есть
            if (firePoint != null && ps.transform.parent != firePoint)
            {
                ps.transform.SetParent(firePoint);
                ps.transform.localPosition = Vector3.zero;
            }

            // Запускаем
            if (!ps.isPlaying)
            {
                ps.Clear();
                ps.Play();
            }
        }

        Debug.Log($"Bonfire: {particles.Length} particle systems started");
    }

    private void Update()
    {
        UpdateFlicker();
        RestoreSanityNearby();
    }

    private void UpdateFlicker()
    {
        if (fireLight == null) return;

        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed, 0f);
        fireLight.intensity = baseIntensity
            + (noise - 0.5f) * flickerAmount * baseIntensity;
    }

    private void RestoreSanityNearby()
    {
        var colliders = Physics.OverlapSphere(
            transform.position, sanityRestoreRadius);

        foreach (var col in colliders)
        {
            var sanity = col.GetComponent<PlayerSanity>();
            if (sanity != null)
            {
                sanity.RestoreSanity(
                    sanityRestoreRate * Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Радиус света
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(
            transform.position, lightRadius);

        // Радиус восстановления рассудка
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(
            transform.position, sanityRestoreRadius);

        // FirePoint
        Transform fp = transform.Find("FirePoint");
        if (fp != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(fp.position, 0.1f);
        }
    }
}