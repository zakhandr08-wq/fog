using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class SeekerCameraCull : MonoBehaviour
{
    [SerializeField] private Renderer[] seekerBodyRenderers;

    private static readonly int EnableClipID =
        Shader.PropertyToID("_EnableClip");

    private MaterialPropertyBlock propertyBlock;
    private Camera myCamera;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        myCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering +=
            OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering +=
            OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -=
            OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -=
            OnEndCameraRendering;
    }

    private void OnBeginCameraRendering(
        ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != myCamera) return;
        SetClip(1f);
    }

    private void OnEndCameraRendering(
        ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != myCamera) return;
        SetClip(0f);
    }

    private void SetClip(float value)
    {
        if (seekerBodyRenderers == null) return;

        foreach (var rend in seekerBodyRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(EnableClipID, value);
            rend.SetPropertyBlock(propertyBlock);
        }
    }
}