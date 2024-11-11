using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MetaballRenderPass : ScriptableRenderPass
{
    private Material material;
    private Vector4[] metaballDataArray = new Vector4[256]; 
    // Fixed array smaller than the daniel's method because of postion and color size being too big for webgl
    public float outlineSize;
    private Vector4[] metaballColor = new Vector4[256];
    public Color outlineColor;

    private bool isFirstRender = true;

    private RenderTargetIdentifier source;
    private string profilerTag;

    public MetaballRenderPass(string profilerTag)
    {
        this.profilerTag = profilerTag;
        material = new Material(Shader.Find("Custom/InferenceMetaballs2D"));
    }

    private void ApplyShaderWithNoDepth(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dst, Material mat)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 0);
        RenderTexture tempRT = RenderTexture.GetTemporary(descriptor);

        cmd.Blit(src, tempRT);
        cmd.Blit(tempRT, dst, mat);

        RenderTexture.ReleaseTemporary(tempRT);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera)
        {
            return;
        }

        if (!renderingData.postProcessingEnabled)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        if (isFirstRender)
        {
            isFirstRender = false;
            cmd.SetGlobalVectorArray("_MetaballData", metaballDataArray);
        }

        List<Metaballs2D> metaballs = MetaballSystem2D.Get();

        for (int i = 0; i < metaballs.Count; ++i)
        {
            Vector2 pos = renderingData.cameraData.camera.WorldToScreenPoint(metaballs[i].transform.position);
            float radius = metaballs[i].GetRadius();
            metaballDataArray[i] = new Vector4(pos.x, pos.y, radius, 0.0f);
            metaballColor[i] = new Vector4(metaballs[i].color.r, metaballs[i].color.g, metaballs[i].color.b, metaballs[i].color.a);
        }

        cmd.SetGlobalInt("_MetaballCount", metaballs.Count);
        cmd.SetGlobalVectorArray("_MetaballData", metaballDataArray);
        cmd.SetGlobalFloat("_OutlineSize", outlineSize);
        cmd.SetGlobalVectorArray("_MetaballColor", metaballColor);
        cmd.SetGlobalColor("_OutlineColor", outlineColor);
        cmd.SetGlobalFloat("_CameraSize", renderingData.cameraData.camera.orthographicSize);

        source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        ApplyShaderWithNoDepth(cmd, source, source, material);

        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}
