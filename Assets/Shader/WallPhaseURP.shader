Shader "Custom/WallPhaseHoloURP"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Color Grading (Normal mode))]
        _Contrast ("Contrast", Range(0.0, 2.0)) = 1.0
        _Saturation ("Saturation", Range(0.0, 2.0)) = 1.0
        _Brightness ("Brightness", Range(0.0, 2.0)) = 1.0

        [Header(Phase Switch)]
        _Phase ("Phase (0=solid, 1=holo)", Range(0,1)) = 0

        [Header(Holo Look)]
        _HoloColor ("Holo Color", Color) = (0.70,0.85,0.80,1)
        _HoloAlpha ("Holo Alpha", Range(0,1)) = 0.25
        
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0, 4)) = 1.5

        [Header(Grid Lines (fake wireframe))]
        _GridScale ("Grid Scale", Range(0.1, 20)) = 4.0
        _GridThickness ("Grid Thickness", Range(0.001, 0.2)) = 0.04
        _GridIntensity ("Grid Intensity", Range(0, 4)) = 1.2

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.55,0.95,0.85,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.05)) = 0.01
        _OutlineAlpha ("Outline Alpha", Range(0,1)) = 0.6

        [Header(Render)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        // ---------- PASS 1: Main (solid or holo fill) ----------
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Contrast, _Saturation, _Brightness;

                float _Phase;
                float4 _HoloColor;
                float _HoloAlpha;

                float _RimPower, _RimIntensity;

                float _GridScale, _GridThickness, _GridIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                half fogFactor    : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 ApplyColorGrading(float3 col, float contrast, float saturation, float brightness)
            {
                col *= brightness;
                col = (col - 0.5) * contrast + 0.5;
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(luma.xxx, col, saturation);
                return col;
            }

            // 3D grid lines in object space: looks like “wireframe inside”
            float GridLines(float3 posOS, float scale, float thickness)
            {
                float3 p = abs(frac(posOS * scale) - 0.5);  // 0..0.5
                float3 d = smoothstep(0.5 - thickness, 0.5, p); // near cell edges -> 1
                float lines = max(d.x, max(d.y, d.z));
                return lines;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float t = saturate(_Phase);

                // base albedo
                float3 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                // normal mode: graded + lit
                float3 graded = ApplyColorGrading(baseCol, _Contrast, _Saturation, _Brightness);

                // simple lighting (only for normal mode)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 n = normalize(IN.normalWS);
                float ndotl = saturate(dot(n, normalize(mainLight.direction)));
                float3 litNormal = graded * (mainLight.color.rgb * ndotl);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lc = GetAdditionalLightsCount();
                for (uint i = 0u; i < lc; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    float ndl = saturate(dot(n, normalize(l.direction)));
                    litNormal += graded * (l.color.rgb * ndl);
                }
                #endif

                litNormal = MixFog(litNormal, IN.fogFactor);

                // holo mode: fill + rim + grid (no “real” lighting)
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float fresnel = pow(1.0 - saturate(dot(n, V)), _RimPower);

                float grid = GridLines(IN.positionOS, _GridScale, _GridThickness);

                float3 holo = _HoloColor.rgb;
                holo += holo * (fresnel * _RimIntensity);
                holo += holo * (grid * _GridIntensity);

                // blend between modes
                float3 finalRGB = lerp(litNormal, holo, t);

                // alpha: solid=1, holo=_HoloAlpha boosted a bit by rim/grid
                float holoA = saturate(_HoloAlpha + fresnel * 0.25 + grid * 0.25);
                float finalA = lerp(1.0, holoA, t);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }

        // ---------- PASS 2: Outline (extruded) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Phase;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float t = saturate(_Phase);

                // Extrude along normal only when phase enabled
                float3 posOS = IN.positionOS.xyz + (IN.normalOS * (_OutlineWidth * t));
                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                OUT.positionCS = pos.positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = saturate(_Phase);
                // Outline visible only in holo mode
                return half4(_OutlineColor.rgb, _OutlineAlpha * t);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
