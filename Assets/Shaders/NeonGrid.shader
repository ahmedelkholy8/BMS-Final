Shader "Unlit/NeonGrid"
{
    Properties
    {
        _BgColor        ("Background Color", Color) = (0.03, 0.03, 0.05, 1)
        _GridColorA     ("Grid Color A (center)", Color) = (0, 0.94, 1, 1)
        _GridColorB     ("Grid Color B (edge)", Color) = (1, 0.5, 0, 1)
        _GridSize       ("Grid Size", Float) = 1.0
        _LineWidth      ("Line Width", Float) = 0.03
        _GradientCenter ("Center (XZ)", Vector) = (0, 0, 0, 0)
        _GradientScale  ("Gradient Radius", Float) = 10.0
        _GradientOffset ("Radius Offset", Float) = 0.0
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _BgColor;
            float4 _GridColorA;
            float4 _GridColorB;
            float  _GridSize;
            float  _LineWidth;
            float4 _GradientCenter;  // xy = center in world XZ
            float  _GradientScale;
            float  _GradientOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- Grid line detection (unchanged) ---
                float2 pos = i.worldPos.xz;
                float2 dist = abs(frac(pos / _GridSize + 0.5) - 0.5) * _GridSize;
                float minDist = min(dist.x, dist.y);
                float gridLine = 1.0 - smoothstep(0.0, _LineWidth, minDist);

                // --- Circular / Radial gradient for grid lines ---
                float2 center = _GradientCenter.xz;
                float radius = distance(i.worldPos.xz, center);
                // t = 0 at (center + offset), t = 1 at (center + offset + scale)
                float t = (radius + _GradientOffset) / _GradientScale;
                t = saturate(t);  // clamp between 0 and 1

                float3 gridColor = lerp(_GridColorA.rgb, _GridColorB.rgb, t);
                // Keep the 2x multiplier for a neon bloom effect
                gridColor *= 2.0;

                // --- Combine with background ---
                float3 col = lerp(_BgColor.rgb, gridColor, gridLine);
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}