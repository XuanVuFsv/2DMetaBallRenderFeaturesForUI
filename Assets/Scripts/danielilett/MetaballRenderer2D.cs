using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Danielilett
{
    public class MetaballRenderer2D : ScriptableRendererFeature
    {
        [System.Serializable]
        public class MetaballRender2DSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

            [Range(0f, 1f), Tooltip("Outline size.")]
            public float outlineSize = 1.0f;

            [Tooltip("Inner color.")]
            public Color innerColor = Color.white;

            [Tooltip("Outline color.")]
            public Color outlineColor = Color.black;
        }

        public MetaballRender2DSettings settings = new();

        private MetaballRender2DPass pass;

        public override void Create()
        {
            name = "Danielilett Metaballs 2D";

            pass = new MetaballRender2DPass("Metaballs2D")
            {
                outlineSize = settings.outlineSize,
                innerColor = settings.innerColor,
                outlineColor = settings.outlineColor,

                renderPassEvent = settings.renderPassEvent
            };
        }
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            pass.Setup();
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }

        // Methods to update settings during runtime
        public void SetOutlineSize(float size)
        {
            settings.outlineSize = size;
            if (pass != null)
            {
                pass.outlineSize = size;
            }
        }

        public void SetInnerColor(Color color)
        {
            settings.innerColor = color;
            if (pass != null)
            {
                pass.innerColor = color;
            }
        }

        public void SetOutlineColor(Color color)
        {
            settings.outlineColor = color;
            if (pass != null)
            {
                pass.outlineColor = color;
            }
        }

        public Color GetOutlineColor()
        {
            return settings.outlineColor;
        }

        public Color GetInnerColor()
        {
            return settings.innerColor;
        }
    }
}
