using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlurBased
{
    public class BlurRenderPass : ScriptableRenderPass
    {
        private static readonly int BlurOffsets = Shader.PropertyToID("_BlurOffsets");
        private static readonly int TempTextureID = Shader.PropertyToID("_TempBlurTexture");
        private static readonly int DownSampledTextureID = Shader.PropertyToID("_DownSampledTexture");

        public readonly Material blurMaterial;
        public readonly Material cutOutMaterial;
        public int iterations = 3;
        public float blurSpread = 0.6f;

        private RenderTextureDescriptor textureDescriptor;

        public BlurRenderPass(Material blurMat, Material cutOutMat)
        {
            this.blurMaterial = blurMat;
            this.cutOutMaterial = cutOutMat;
            textureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            textureDescriptor.width = cameraTextureDescriptor.width;
            textureDescriptor.height = cameraTextureDescriptor.height;
            textureDescriptor.depthBufferBits = 0; // No depth buffer needed for blur

            // Allocate temporary render textures using CommandBuffer
            cmd.GetTemporaryRT(TempTextureID, textureDescriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(DownSampledTextureID, textureDescriptor, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
            var isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;

            if (!isPostProcessEnabled || isSceneViewCamera)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Blur Effect");

            // Get the camera color target
            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

            // Down sample to quarter resolution
            DownSample4X(cmd, source, DownSampledTextureID);

            // Apply blur iterations
            for (int i = 0; i < iterations; i++)
            {
                FourTapCone(cmd, DownSampledTextureID, TempTextureID, i);
                // Swap the textures for the next iteration
                cmd.CopyTexture(TempTextureID, DownSampledTextureID);
            }

            // Apply the blurred texture back to the camera target
            cmd.Blit(DownSampledTextureID, source, cutOutMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Release temporary render textures
            cmd.ReleaseTemporaryRT(TempTextureID);
            cmd.ReleaseTemporaryRT(DownSampledTextureID);
        }

        // Performs one blur iteration using multi-tap sampling
        private void FourTapCone(CommandBuffer cmd, int source, int dest, int iteration)
        {
            var off = 0.5f + iteration * blurSpread;
            blurMaterial.SetVector(BlurOffsets, new Vector4(-off, -off, off, off));
            cmd.Blit(source, dest, blurMaterial);
        }

        // Down samples the texture to a quarter resolution
        private void DownSample4X(CommandBuffer cmd, RenderTargetIdentifier source, int dest)
        {
            const float off = 1.0f;
            blurMaterial.SetVector(BlurOffsets, new Vector4(-off, -off, off, off));
            cmd.Blit(source, dest, blurMaterial);
        }
    }
}