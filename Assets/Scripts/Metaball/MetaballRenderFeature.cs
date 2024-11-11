using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MetaballRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MetaballRender2DSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Range(0f, 1f), Tooltip("Outline size.")]
        public float outlineSize = 1.0f;

        [Tooltip("Outline color.")]
        public Color outlineColor = Color.black;
    }

    public MetaballRender2DSettings settings = new MetaballRender2DSettings();
    MetaballRenderPass pass;

    public override void Create()
    {
        name = "Inference Metaballs 2d";

        pass = new MetaballRenderPass("Metaballs2D")
        {
            outlineSize = settings.outlineSize,
            outlineColor = settings.outlineColor,

            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}
