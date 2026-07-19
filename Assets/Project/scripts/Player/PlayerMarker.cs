using UnityEngine;

public class PlayerMarker : MonoBehaviour
{
    private float markTimer;
    private bool isMarked;
    private GameObject vfxInstance;

    public bool IsMarked => isMarked;
    public float MarkTimeRemaining => markTimer;

    private void Update()
    {
        if (!isMarked) return;

        markTimer -= Time.deltaTime;

        if (markTimer <= 0f)
        {
            RemoveMark();
        }
    }

    public void Mark(float duration, GameObject vfxPrefab)
    {
        markTimer = duration;
        isMarked = true;

        if (vfxPrefab != null && vfxInstance == null)
        {
            vfxInstance = Instantiate(
                vfxPrefab, transform);
            vfxInstance.transform.localPosition =
                Vector3.up * 2.2f;
        }

        Debug.Log($"{name} помечен на {duration}с");
    }

    public void RemoveMark()
    {
        isMarked = false;
        markTimer = 0f;

        if (vfxInstance != null)
        {
            Destroy(vfxInstance);
            vfxInstance = null;
        }

        Debug.Log($"{name} Ч метка сн€та");
    }
}