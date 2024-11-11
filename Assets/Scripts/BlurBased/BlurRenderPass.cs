using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlurBased
{
    public class BlurRenderPass : ScriptableRenderPass
    {
        private static readonly int BlurOffsets = Shader.PropertyToID("_BlurOffsets");
        public readonly Material blurMaterial;
        public readonly Material cutOutMaterial;

        public int iterations = 3;
        public float blurSpread = 0.6f;

        private RTHandle tempTexture;
        private RTHandle downSampledTexture;
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

            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, textureDescriptor);
            RenderingUtils.ReAllocateIfNeeded(ref downSampledTexture, textureDescriptor);
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

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            DownSample4X(cmd, source, downSampledTexture);

            for (int i = 0; i < iterations; i++)
            {
                FourTapCone(cmd, downSampledTexture, tempTexture, i);
                // Swap the textures for the next iteration
                cmd.CopyTexture(tempTexture, downSampledTexture);
            }

            cmd.Blit(downSampledTexture, source, cutOutMaterial); // Apply the blurred texture

            // Release temporary render textures
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Performs one blur iteration using multi-tap sampling
        private void FourTapCone(CommandBuffer cmd, RTHandle source, RTHandle dest, int iteration)
        {
            var off = 0.5f + iteration * blurSpread;
            blurMaterial.SetVector(BlurOffsets, new Vector4(-off, -off, off, off));
            cmd.Blit(source, dest, blurMaterial);
        }

        // Down samples the texture to a quarter resolution
        private void DownSample4X(CommandBuffer cmd, RTHandle source, RTHandle dest)
        {
            const float off = 1.0f;
            blurMaterial.SetVector(BlurOffsets, new Vector4(-off, -off, off, off));
            cmd.Blit(source, dest, blurMaterial);
        }
    }
}

