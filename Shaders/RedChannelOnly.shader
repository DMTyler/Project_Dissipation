Shader "Hidden/DGraphics/RedChannelOnly"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always 
            Cull Off 
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            struct VertIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct VertOut
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VertOut vert (VertIn v)
            {
                VertOut o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (VertOut i) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return float4(c.r, 0, 0, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
