Shader "ScienceTeacher/MRGlass"
{
    Properties
    {
        [MainColor] _BaseColor("Tint and opacity", Color) = (0.7, 0.9, 0.94, 0.16)
        _RimColor("Edge highlight", Color) = (0.55, 0.94, 1, 1)
        _RimStrength("Edge opacity", Range(0, 1)) = 0.42
        _EmissionColor("Selection highlight", Color) = (0, 0, 0, 0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Glass"
            Tags { "LightMode"="UniversalForward" }
            // Preserve coverage in the eye texture for the passthrough underlay.
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half4 _EmissionColor;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = saturate(abs(dot(normal, view)));
                half edge = pow(1.0h - facing, 3.0h);
                // A broad softbox glint is stable on mobile without a reflection
                // camera, scene-color sampling, bloom, or per-eye full-screen passes.
                half3 lightDirection = normalize(half3(-0.35h, 0.82h, -0.45h));
                half glint = pow(saturate(abs(dot(normal, normalize(view + lightDirection)))), 64.0h);
                half3 color = lerp(_BaseColor.rgb, _RimColor.rgb, edge * 0.85h);
                color += glint * 0.25h + _EmissionColor.rgb * 0.25h;
                half alpha = saturate(_BaseColor.a + (1.0h - _BaseColor.a) * edge * _RimStrength + glint * 0.06h);
                return half4(saturate(color), alpha);
            }
            ENDHLSL
        }
    }
}
