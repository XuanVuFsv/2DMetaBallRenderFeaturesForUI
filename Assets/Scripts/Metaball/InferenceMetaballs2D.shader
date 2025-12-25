Shader "Custom/InferenceMetaballs2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            int _MetaballCount;
            float4 _MetaballData[256]; // x,y = screen pos, z = radius, w = texture index (-1 = no texture)
            float4 _MetaballColor[256];
            float _OutlineSize;
            float4 _OutlineColor;
            float _BlendFalloff;
            
            // Texture samplers (max 256 but practically limited by shader)
            sampler2D _MetaballTex0;
            sampler2D _MetaballTex1;
            sampler2D _MetaballTex2;
            sampler2D _MetaballTex3;
            sampler2D _MetaballTex4;
            sampler2D _MetaballTex5;
            sampler2D _MetaballTex6;
            sampler2D _MetaballTex7;
            // Add more if needed...
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.z = 0;
                o.uv = v.uv;
                return o;
            }
            
            float4 SampleMetaballTexture(int index, float2 uv)
            {
                // Sample the correct texture based on index
                if (index == 0) return tex2D(_MetaballTex0, uv);
                if (index == 1) return tex2D(_MetaballTex1, uv);
                if (index == 2) return tex2D(_MetaballTex2, uv);
                if (index == 3) return tex2D(_MetaballTex3, uv);
                if (index == 4) return tex2D(_MetaballTex4, uv);
                if (index == 5) return tex2D(_MetaballTex5, uv);
                if (index == 6) return tex2D(_MetaballTex6, uv);
                if (index == 7) return tex2D(_MetaballTex7, uv);
                // Add more cases if needed...
                return float4(1, 1, 1, 1);
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float4 originalTex = tex2D(_MainTex, i.uv);
                
                float totalInfluence = 0.0;
                float4 weightedColor = float4(0, 0, 0, 0);
                float2 pixelPos = i.uv * _ScreenParams.xy;
                
                for (int m = 0; m < _MetaballCount; ++m)
                {
                    float2 metaballPos = _MetaballData[m].xy;
                    float radiusInPixels = _MetaballData[m].z;
                    float textureIndex = _MetaballData[m].w;
                    
                    // Calculate distance
                    float2 diff = pixelPos - metaballPos;
                    float dist = length(diff);
                    
                    // Base influence formula
                    float influence = radiusInPixels / max(dist, 1.0);
                    
                    // Apply falloff
                    influence = pow(influence, _BlendFalloff);
                    
                    // Calculate UV for texture sampling (relative to metaball center)
                    float2 localUV = (diff / radiusInPixels) * 0.5 + 0.5; // Map to 0-1
                    
                    // Get color from texture or solid color
                    float4 metaballCol;
                    if (textureIndex >= 0)
                    {
                        // Sample texture
                        float4 texColor = SampleMetaballTexture((int)textureIndex, localUV);
                        metaballCol = texColor * _MetaballColor[m];
                    }
                    else
                    {
                        // Use solid color
                        metaballCol = _MetaballColor[m];
                    }
                    
                    totalInfluence += influence;
                    weightedColor += metaballCol * influence;
                }
                
                float threshold = 1.0;
                
                if (totalInfluence < 0.01)
                {
                    return originalTex;
                }
                
                float4 metaballColor = weightedColor / max(totalInfluence, 0.0001);
                
                if (totalInfluence > threshold)
                {
                    return metaballColor;
                }
                else if (_OutlineSize > 0.001 && totalInfluence > (threshold - _OutlineSize))
                {
                    float outlineStart = threshold - _OutlineSize;
                    float outlineEnd = threshold;
                    float t = smoothstep(outlineStart, outlineEnd, totalInfluence);
                    return lerp(_OutlineColor, metaballColor, t);
                }
                else
                {
                    return originalTex;
                }
            }
            ENDCG
            ZTest Always
        }
    }
}