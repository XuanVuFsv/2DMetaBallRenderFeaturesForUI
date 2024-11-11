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
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            int _MetaballCount;
            float3 _MetaballData[256];
            float4 _MetaballColor[256];
            float _OutlineSize;
            float4 _OutlineColor;
            float _CameraSize;

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
                o.vertex.z = 0; // Set Z to zero
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float4 col = float4(0, 0, 0, 0);
                float infl = 0.0f;

                for (int m = 0; m < _MetaballCount; ++m)
                {
                    float2 metaballPos = _MetaballData[m].xy;
                    float distFromMetaball = length(metaballPos - (i.uv * _ScreenParams.xy));
                    float radiusSize = _MetaballData[m].z * _ScreenParams.y / _CameraSize;
                    float currInfl = radiusSize * radiusSize;
                    currInfl /= (pow(abs((i.uv.x * _ScreenParams.x) - metaballPos.x), 2.0) + pow(abs((i.uv.y * _ScreenParams.y) - metaballPos.y), 2.0));
                    infl += currInfl;
                    col += _MetaballColor[m] * currInfl;
                }

                float threshold = 0.95f;
                if (infl-0.5 > threshold)
                    col = normalize(col);
                else if (infl > threshold- _OutlineSize)
                {
                    float4 colorToMixWith = _OutlineColor;
                    float smoothStep1 = smoothstep(threshold + 0.5, threshold, infl);
                    float smoothStep2 = smoothstep(threshold - 0.5, threshold, infl);
                    float blendFactor = smoothStep1 * smoothStep2;
                    
                    col = lerp(col, colorToMixWith, blendFactor);
                }
                else
                {
                    col = tex;
                }
                return col;
            }
            ENDCG
            ZTest Always
        }
    }
}
