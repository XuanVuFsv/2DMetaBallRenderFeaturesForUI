Shader "Custom/Metaballs2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            int _MetaballCount;
            float3 _MetaballData[1000]; // Fixed array size
            float _OutlineSize;
            float4 _InnerColor;
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

                float dist = 1.0f;

                for (int m = 0; m < _MetaballCount; ++m)
                {
                    float2 metaballPos = _MetaballData[m].xy;

                    float distFromMetaball = distance(metaballPos, i.uv * _ScreenParams.xy);
                    float radiusSize = _MetaballData[m].z * _ScreenParams.y / _CameraSize;

                    dist *= saturate(distFromMetaball / radiusSize);
                }

                float threshold = 0.5f;
                float outlineThreshold = threshold * (1.0f - _OutlineSize);

                return (dist > threshold) ? tex :
                    ((dist > outlineThreshold) ? _OutlineColor : _InnerColor);
            }
            ENDCG
            ZTest Always
        }
    }
}
