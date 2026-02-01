Shader "Custom/BlurEffectConeTap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float2 tap0 : TEXCOORD1;
                float2 tap1 : TEXCOORD2;
                float2 tap2 : TEXCOORD3;
                float2 tap3 : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurOffsets;        

            v2f vert (appdata v)
            {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv - _BlurOffsets.xy * _MainTex_TexelSize.xy;
                o.tap0 = o.uv + _MainTex_TexelSize.xy * _BlurOffsets.xy;
                o.tap1 = o.uv - _MainTex_TexelSize.xy * _BlurOffsets.xy;
                o.tap2 = o.uv + _MainTex_TexelSize.xy * _BlurOffsets.xy * float2(1,-1);
                o.tap3 = o.uv - _MainTex_TexelSize.xy * _BlurOffsets.xy * float2(1,-1);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.tap0);
                color += tex2D(_MainTex, i.tap1);
                color += tex2D(_MainTex, i.tap2);
                color += tex2D(_MainTex, i.tap3); 
                return color * 0.25;
            }
            ENDCG
        }
    }
    Fallback off
}
