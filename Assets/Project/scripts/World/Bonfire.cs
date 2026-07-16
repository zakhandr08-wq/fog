using UnityEngine;

public class Bonfire : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light fireLight;
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

    private float baseIntensity;

    private void Start()
    {
        if (fireLight == null)
        {
            // Create light
            var lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition =
                Vector3.up * 0.5f;

            fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
        }

        fireLight.range = lightRadius;
        fireLight.intensity = lightIntensity;
        fireLight.color = lightColor;
        baseIntensity = lightIntensity;
    }

    private void Update()
    {
        // Flicker effect
        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed, 0f);
        fireLight.intensity = baseIntensity
            + (noise - 0.5f) * flickerAmount * baseIntensity;

        // Restore sanity to nearby players
        RestoreSanityNearby();
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position, sanityRestoreRadius);
    }
}
