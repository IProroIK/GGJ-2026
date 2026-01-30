Shader "Custom/BlueprintOptimizedLocalGrid_Dissolve"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.0, 0.6, 1.0, 1.0)
        _GridColor("Grid Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Transparency("Transparency", Range(0,1)) = 0.5
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 2.0
        _GridScale("Grid Scale", Float) = 10.0
        _GridThickness("Grid Thickness", Float) = 0.1
        _OutlineColor("Outline Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _OutlineStrength("Outline Strength", Range(0, 5)) = 1.5

        // --- Minimal dissolve ---
        _NoiseTex("Dissolve Noise (R)", 2D) = "white" {}
        _Dissolve("Dissolve (0=invisible, 1=visible)", Range(0,1)) = 1.0
        _DissolveSoft("Dissolve Softness", Range(0.0001, 0.5)) = 0.08
        _NoiseScale("Noise Scale", Float) = 2.0

        _EdgeColor("Dissolve Edge Color", Color) = (0.8, 1.0, 1.0, 1.0)
        _EdgeWidth("Dissolve Edge Width", Range(0.0, 0.3)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
                float3 localPos     : TEXCOORD2;
            };

            float4 _BaseColor;
            float4 _GridColor;
            float _Transparency;
            float _FresnelPower;
            float _GridScale;
            float _GridThickness;
            float4 _OutlineColor;
            float _OutlineStrength;

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float4 _NoiseTex_ST;

            float _Dissolve;
            float _DissolveSoft;
            float _NoiseScale;
            float4 _EdgeColor;
            float _EdgeWidth;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos;
                OUT.localPos = IN.positionOS.xyz;
                OUT.worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionCS = TransformWorldToHClip(worldPos);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Fresnel
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float ndotv = saturate(dot(IN.worldNormal, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower);

                // Grid
                float3 scaledPos = IN.localPos * _GridScale;
                float3 grid = abs(frac(scaledPos - 0.5) - 0.5) / max(fwidth(scaledPos), 1e-5);
                float gridLine = min(min(grid.x, grid.y), grid.z);
                float gridMask = 1.0 - smoothstep(0.0, _GridThickness, gridLine);

                // --- Dissolve mask (world XZ to avoid UV seams on cube) ---
                float2 uv = IN.worldPos.xz * _NoiseScale;
                uv = uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;

                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv).r;

                // _Dissolve: 0..1 (0 invisible, 1 visible)
                float t0 = _Dissolve - _DissolveSoft;
                float t1 = _Dissolve + _DissolveSoft;
                float mask = smoothstep(t0, t1, noise);

                // Clip for clean dissolve on transparent
                clip(mask - 0.001);

                // Edge glow near threshold
                float edge = 1.0 - smoothstep(_Dissolve, _Dissolve + max(_EdgeWidth, 1e-5), noise);

                // Base color + outline
                float3 baseColor = _BaseColor.rgb + _GridColor.rgb * gridMask;
                float3 outline = _OutlineColor.rgb * saturate(fresnel * _OutlineStrength);

                float3 finalColor = baseColor + outline + _EdgeColor.rgb * edge;

                float alpha = saturate(_Transparency + fresnel * 0.5);

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
