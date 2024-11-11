using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Danielilett
{
    public class MetaballRender2DPass : ScriptableRenderPass
    {
        private static readonly int MetaballData = Shader.PropertyToID("_MetaballData");
        private static readonly int MetaballCount = Shader.PropertyToID("_MetaballCount");
        private static readonly int OutlineSize = Shader.PropertyToID("_OutlineSize");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int CameraSize = Shader.PropertyToID("_CameraSize");
        private Material material;
        private readonly Vector4[] metaballDataArray = new Vector4[1000]; // Fixed array size

        public float outlineSize;
        public Color innerColor;
        public Color outlineColor;

        private bool isFirstRender = true;

        private RenderTargetIdentifier source;
        private readonly string profilerTag;

        public void Setup()
        {
            material = new Material(Shader.Find("Custom/Metaballs2D"));
        }

        public MetaballRender2DPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
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
            var isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
            var isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;

            if (!isPostProcessEnabled || isSceneViewCamera)
            {
                return;
            }

            var cmd = CommandBufferPool.Get(profilerTag);

            if (isFirstRender)
            {
                isFirstRender = false;
                cmd.SetGlobalVectorArray(MetaballData, metaballDataArray);
            }

            List<Metaballs2D> metaballs = MetaballSystem2D.Get();

            for (int i = 0; i < metaballs.Count; ++i)
            {
                Vector2 pos = renderingData.cameraData.camera.WorldToScreenPoint(metaballs[i].transform.position);
                float radius = metaballs[i].GetRadius();
                metaballDataArray[i] = new Vector4(pos.x, pos.y, radius, 0.0f);
            }

            source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            cmd.SetGlobalInt(MetaballCount, metaballs.Count);
            cmd.SetGlobalVectorArray(MetaballData, metaballDataArray);
            cmd.SetGlobalFloat(OutlineSize, outlineSize);
            cmd.SetGlobalColor(InnerColor, innerColor);
            cmd.SetGlobalColor(OutlineColor, outlineColor);
            cmd.SetGlobalFloat(CameraSize, renderingData.cameraData.camera.orthographicSize);

            ApplyShaderWithNoDepth(cmd, source, source, material);

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}