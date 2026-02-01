using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MetaballRenderPass : ScriptableRenderPass
{
    private Material material;
    private Vector4[] metaballDataArray = new Vector4[256];
    public float outlineSize;
    private Vector4[] metaballColor = new Vector4[256];
    public Color outlineColor;
    public float blendFalloff = 1.0f;
    private bool isFirstRender = true;
    private RenderTargetIdentifier source;
    private string profilerTag;

    // Texture array support
    private Texture2DArray textureArray;
    private List<Texture2D> textureList = new List<Texture2D>();
    private bool textureArrayNeedsUpdate = true;

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

    private void UpdateTextureArray(List<Metaballs2D> metaballs)
    {
        textureList.Clear();

        // Collect all textures
        for (int i = 0; i < metaballs.Count; ++i)
        {
            Texture2D tex = metaballs[i].GetTexture();
            textureList.Add(tex); // Can be null for solid colors
        }

        textureArrayNeedsUpdate = false;
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

        if (metaballs.Count == 0)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            return;
        }

        Camera cam = renderingData.cameraData.camera;

        // Mark which metaballs should render based on connection distance
        bool[] shouldRender = new bool[metaballs.Count];

        // Cache positions to avoid recalculating
        Vector3[] screenPositions = new Vector3[metaballs.Count];
        Vector2[] worldPositions = new Vector2[metaballs.Count];

        for (int i = 0; i < metaballs.Count; ++i)
        {
            worldPositions[i] = metaballs[i].GetWorldPosition();
            screenPositions[i] = cam.WorldToScreenPoint(worldPositions[i]);
        }

        // If only 1 metaball, render it
        if (metaballs.Count == 1)
        {
            shouldRender[0] = true;
        }
        else
        {
            // Check each pair of metaballs
            for (int i = 0; i < metaballs.Count; ++i)
            {
                for (int j = i + 1; j < metaballs.Count; ++j)
                {
                    // Calculate distance for metaball i
                    float distanceI;
                    if (metaballs[i].GetMetaballType() == Metaballs2D.MetaballType.UI)
                    {
                        // UI: pixel distance
                        distanceI = Vector2.Distance(screenPositions[i], screenPositions[j]);
                    }
                    else
                    {
                        // Sprite: world distance
                        distanceI = Vector2.Distance(worldPositions[i], worldPositions[j]);
                    }

                    // Calculate distance for metaball j
                    float distanceJ;
                    if (metaballs[j].GetMetaballType() == Metaballs2D.MetaballType.UI)
                    {
                        // UI: pixel distance
                        distanceJ = Vector2.Distance(screenPositions[i], screenPositions[j]);
                    }
                    else
                    {
                        // Sprite: world distance
                        distanceJ = Vector2.Distance(worldPositions[i], worldPositions[j]);
                    }

                    // If either is close enough to its threshold, render both
                    if (distanceI <= metaballs[i].GetConnectionDistance() ||
                        distanceJ <= metaballs[j].GetConnectionDistance())
                    {
                        shouldRender[i] = true;
                        shouldRender[j] = true;
                    }
                }
            }
        }

        // Update texture list
        UpdateTextureArray(metaballs);

        // Build array with only metaballs that should render
        int activeCount = 0;
        for (int i = 0; i < metaballs.Count; ++i)
        {
            if (!shouldRender[i])
                continue;

            Vector2 worldPos = worldPositions[i];
            Vector3 screenPos = screenPositions[i];

            // Get radius - already calculated correctly for both UI and World objects
            float worldRadius = metaballs[i].GetRadius();

            // Convert radius to screen pixels
            Vector3 worldPosOffset = worldPos + Vector2.right * worldRadius;
            Vector3 screenPosOffset = cam.WorldToScreenPoint(worldPosOffset);
            float radiusInPixels = Vector2.Distance(screenPos, screenPosOffset);

            // Store texture index (or -1 for no texture)
            float textureIndex = metaballs[i].HasTexture() ? i : -1f;

            metaballDataArray[activeCount] = new Vector4(screenPos.x, screenPos.y, radiusInPixels, textureIndex);

            Color col = metaballs[i].GetColor();
            metaballColor[activeCount] = new Vector4(col.r, col.g, col.b, col.a);

            // Set individual texture if available
            Texture2D tex = textureList[i];
            if (tex != null)
            {
                cmd.SetGlobalTexture($"_MetaballTex{activeCount}", tex);
            }

            activeCount++;
        }

        // Only render if at least 1 metaball is active
        if (activeCount > 0)
        {
            cmd.SetGlobalInt("_MetaballCount", activeCount);
            cmd.SetGlobalVectorArray("_MetaballData", metaballDataArray);
            cmd.SetGlobalFloat("_OutlineSize", outlineSize);
            cmd.SetGlobalVectorArray("_MetaballColor", metaballColor);
            cmd.SetGlobalColor("_OutlineColor", outlineColor);
            cmd.SetGlobalFloat("_BlendFalloff", blendFalloff);

            source = renderingData.cameraData.renderer.cameraColorTarget;
            ApplyShaderWithNoDepth(cmd, source, source, material);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}