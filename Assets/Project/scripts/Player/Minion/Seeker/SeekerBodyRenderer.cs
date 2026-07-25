using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SeekerBodyRenderer : MonoBehaviour
{
    [SerializeField] private Camera seekerCamera;

    private Renderer bodyRenderer;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int EnableClipID =
        Shader.PropertyToID("_EnableClip");

    private void Awake()
    {
        bodyRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (seekerCamera == null)
        {
            var seeker =
                GetComponentInParent<SeekerController>();
            if (seeker != null)
            {
                seekerCamera =
                    seeker.GetComponentInChildren<Camera>();
            }
        }
    }

    // Вызывается перед каждой камерой которая
    // собирается рендерить этот объект
    private void OnWillRenderObject()
    {
        if (bodyRenderer == null) return;

        bool isSeekerCam = Camera.current == seekerCamera;
        float enableValue = isSeekerCam ? 1f : 0f;

        Debug.Log($"[{Camera.current?.name}] EnableClip = " +
            $"{enableValue}, seekerCam = {seekerCamera?.name}");

        bodyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(EnableClipID, enableValue);
        bodyRenderer.SetPropertyBlock(propertyBlock);
    }
}