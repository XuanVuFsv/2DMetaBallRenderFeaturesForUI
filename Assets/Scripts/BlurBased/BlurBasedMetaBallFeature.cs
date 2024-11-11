using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlurBased
{
    public class BlurBasedMetaBallFeature : ScriptableRendererFeature
    {
        private BlurRenderPass blurRenderPass;

        public Shader blurShader;
        public Material cutOutMaterial;
        public int iterations = 3;
        public float blurSpread = 0.6f;
        public RenderPassEvent renderPassEvent;
        public override void Create()
        {
            if (blurShader == null)
            {
                Debug.LogError("Blur shader is missing.");
                return;
            }

            Material blurMaterial = new Material(blurShader);
            blurRenderPass = new BlurRenderPass(blurMaterial, cutOutMaterial)
            {
                iterations = iterations,
                blurSpread = blurSpread,
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (blurRenderPass.blurMaterial == null || blurRenderPass.cutOutMaterial == null)
            {
                return;
            }

            renderer.EnqueuePass(blurRenderPass);
        }
    }
}
