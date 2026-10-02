Shader "NetFold/PlaneClip"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.45, 0.78, 1, 0.85)
        _PlanePos("Plane Position", Vector) = (0, 0, 0, 0)
        _PlaneNormal("Plane Normal", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attr
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct V2F
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _PlanePos;
            float4 _PlaneNormal;

            V2F vert(Attr i)
            {
                V2F o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            half4 frag(V2F i) : SV_Target
            {
                float side = dot(i.positionWS - _PlanePos.xyz, normalize(_PlaneNormal.xyz));
                clip(side);
                float fresnel = pow(1.0 - saturate(dot(normalize(i.normalWS), float3(0, 1, 0))), 2.0);
                float3 col = _BaseColor.rgb + fresnel * 0.25;
                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
