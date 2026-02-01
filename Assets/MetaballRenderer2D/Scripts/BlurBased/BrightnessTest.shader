Shader "Custom/BrightnessTest"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Main color", Color) = (1,1,1,1)
        _InnerStroke ("Inner stroke cutoff", Range(0,1)) = 0.3
        _OuterStroke ("Outer stroke cutoff", Range(0,1)) = 0.1
        _LowBrightnessThreshold ("Low brightness threshold", Range(0,0.1)) = 0.01
        _InnerStrokeColor ("Inner stroke color", Color) = (1,1,1,1)
        _OuterStrokeColor ("Outer stroke color", Color) = (0.5,0.5,0.5,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;
            fixed _InnerStroke;
            fixed _OuterStroke;
            fixed _LowBrightnessThreshold;

            half4 _Color;
            half4 _InnerStrokeColor;
            half4 _OuterStrokeColor;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                
                float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114)); 
                /* brightness value that represents how bright the color appears to the human eye,
                accounting for the fact that we perceive green as brighter than red or blue.
                float3(0.299, 0.587, 0.114) are the luminance coefficients for red, green, and blue 
                */

                if (brightness < _LowBrightnessThreshold) {
                    return float4(0, 0, 0, 1);
                }
                
                if (brightness < _OuterStroke)
                {
                    return _OuterStrokeColor;
                } else if (brightness < _InnerStroke)
                {
                    return _InnerStrokeColor;
                } else {
                    return _Color;
                }
            }
            ENDCG
        }
    }
}
