Shader "Custom/MultiColorReplace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Toggle] _EnableReplace ("Enable Replace", Float) = 1

        [Toggle] _EnableReplace1 ("Use Replace 1", Float) = 1
        _ColorFrom1 ("From Color 1", Color) = (1,0,0,1)
        _ColorTo1   ("To Color 1", Color)   = (0,1,0,1)
        _Range1 ("Range 1", Range(0,1)) = 0.1

        [Toggle] _EnableReplace2 ("Use Replace 2", Float) = 0
        _ColorFrom2 ("From Color 2", Color) = (0,0,1,1)
        _ColorTo2   ("To Color 2", Color)   = (1,1,0,1)
        _Range2 ("Range 2", Range(0,1)) = 0.1

        [Toggle] _EnableReplace3 ("Use Replace 3", Float) = 0
        _ColorFrom3 ("From Color 3", Color) = (0,1,1,1)
        _ColorTo3   ("To Color 3", Color)   = (1,0,1,1)
        _Range3 ("Range 3", Range(0,1)) = 0.1

        _Brightness ("Brightness", Range(-1,1)) = 0
        _Saturation ("Saturation", Range(0,2)) = 1
        _Contrast   ("Contrast", Range(0,2)) = 1
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _EnableReplace;
            float _EnableReplace1;
            float _EnableReplace2;
            float _EnableReplace3;

            float4 _ColorFrom1, _ColorTo1;
            float4 _ColorFrom2, _ColorTo2;
            float4 _ColorFrom3, _ColorTo3;

            float _Range1, _Range2, _Range3;

            float _Brightness;
            float _Saturation;
            float _Contrast;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
                return o;
            }

            float luminance(float3 c)
            {
                return dot(c, float3(0.299,0.587,0.114));
            }

            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0., -1./3., 2./3., -1.);
                float4 p = lerp(float4(c.bg, K.wz),
                                float4(c.gb, K.xy),
                                step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r),
                                float4(c.r, p.yzx),
                                step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6. * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1., 2./3., 1./3., 3.);
                float3 p = abs(frac(c.xxx + K.xyz) * 6. - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 ReplaceColor(float3 col, float3 from, float3 to, float range)
            {
                float3 hsv = RGBtoHSV(col);
                float3 fromHSV = RGBtoHSV(from);
                float3 toHSV = RGBtoHSV(to);

                float hueDiff = abs(hsv.x - fromHSV.x);
                hueDiff = min(hueDiff, 1 - hueDiff);

                float mask = saturate(1 - hueDiff / max(range, 0.0001));
                hsv.x = lerp(hsv.x, toHSV.x, mask);

                return HSVtoRGB(hsv);
            }

            float3 ApplyColorAdjust(float3 c)
            {
                c += _Brightness;

                float l = luminance(c);
                c = lerp(l.xxx, c, _Saturation);

                c = (c - 0.5) * _Contrast + 0.5;

                return saturate(c);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float3 col = tex.rgb;

                if (_EnableReplace > 0.5)
                {
                    if (_EnableReplace1 > 0.5)
                        col = ReplaceColor(col, _ColorFrom1.rgb, _ColorTo1.rgb, _Range1);

                    if (_EnableReplace2 > 0.5)
                        col = ReplaceColor(col, _ColorFrom2.rgb, _ColorTo2.rgb, _Range2);

                    if (_EnableReplace3 > 0.5)
                        col = ReplaceColor(col, _ColorFrom3.rgb, _ColorTo3.rgb, _Range3);
                }

                col = ApplyColorAdjust(col);

                return float4(col, tex.a);
            }
            ENDCG
        }
    }
}
