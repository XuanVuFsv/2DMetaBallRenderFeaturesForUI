using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MetaballRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MetaballRender2DSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        [Range(0f, 1f), Tooltip("Outline size.")]
        public float outlineSize = 1.0f;

        [Tooltip("Outline color.")]
        public Color outlineColor = Color.black;

        [Range(0.5f, 5f), Tooltip("Blend falloff - higher values concentrate blending toward center between objects.")]
        public float blendFalloff = 1.0f;
    }

    public MetaballRender2DSettings settings = new MetaballRender2DSettings();
    private MetaballRenderPass pass;

    public override void Create()
    {
        name = "Inference Metaballs 2d";
        pass = new MetaballRenderPass("Metaballs2D")
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Update settings every frame
        pass.outlineSize = settings.outlineSize;
        pass.outlineColor = settings.outlineColor;
        pass.blendFalloff = settings.blendFalloff;

        renderer.EnqueuePass(pass);
    }
}