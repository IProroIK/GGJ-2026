Shader "UI/MaskPopup"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SegmentCount ("Segment Count", Float) = 5
        _SelectedIndex ("Selected Index", Float) = -1
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.62
        _OuterRadius ("Outer Radius", Range(0,1)) = 0.98
        _SeparatorWidth ("Separator Width", Range(0,0.05)) = 0.008
        _BorderWidth ("Border Width", Range(0,0.05)) = 0.012
        _BaseColor ("Segment Color", Color) = (0.19,0.22,0.25,0.88)
        _HoverColor ("Hover Color", Color) = (0.22,0.56,0.68,0.94)
        _LineColor ("Separator Color", Color) = (0.72,0.78,0.8,0.85)
        _AccentColor ("Hover Rim", Color) = (0.25,0.85,1,1)
        _CenterColor ("Center Color", Color) = (0.045,0.065,0.085,0.85)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _Color, _BaseColor, _HoverColor, _LineColor, _AccentColor, _CenterColor;
            float4 _ClipRect;
            float _SegmentCount, _SelectedIndex, _InnerRadius, _OuterRadius, _SeparatorWidth, _BorderWidth;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 radialPosition = (i.uv - 0.5) * 2.0;
                float radius = length(radialPosition);
                float aa = max(fwidth(radius), 0.0001);
                float count = max(1.0, floor(_SegmentCount));
                // Same as MaskPopup: atan2(x,y), zero at top, increasing clockwise.
                float angle = radius > 0.00001 ? atan2(radialPosition.x, radialPosition.y) : 0.0;
                float turns = frac(angle / (2.0 * UNITY_PI) + 1.0);
                float segment = min(floor(turns * count), count - 1.0);
                float selected = 1.0 - step(0.5, abs(segment - _SelectedIndex));
                float4 color = lerp(_BaseColor, _HoverColor, selected);
                float sectorPosition = frac(turns * count);
                float boundaryAngle = min(sectorPosition, 1.0 - sectorPosition) * (2.0 * UNITY_PI / count);
                float boundaryDistance = sin(boundaryAngle) * radius;
                float separator = (1.0 - smoothstep(_SeparatorWidth, _SeparatorWidth + aa, boundaryDistance)) * step(1.5, count);
                color = lerp(color, _LineColor, separator);
                float rim = smoothstep(_OuterRadius - _BorderWidth - aa, _OuterRadius - _BorderWidth, radius);
                color = lerp(color, lerp(_LineColor, _AccentColor, selected), rim);
                float ring = smoothstep(_InnerRadius - aa, _InnerRadius + aa, radius);
                color = lerp(_CenterColor, color, ring);
                color.a *= 1.0 - smoothstep(_OuterRadius - aa, _OuterRadius + aa, radius);
                color *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
