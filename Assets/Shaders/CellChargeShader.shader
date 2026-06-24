Shader "BMS/CellChargeShader"
{
    Properties
    {
        _ChargeColor     ("Liquid Color (full)",  Color)  = (0.13, 0.80, 0.27, 1)
        _WarnColor       ("Liquid Warn Color",    Color)  = (1.00, 0.75, 0.00, 1)
        _LowColor        ("Liquid Low Color",     Color)  = (0.90, 0.10, 0.10, 1)
        _FillLevel       ("Fill Level",           Range(0,1)) = 0.78
        _WarnThreshold   ("Warn Threshold",       Range(0,1)) = 0.35
        _LowThreshold    ("Low Threshold",        Range(0,1)) = 0.20
        _FillSoftness    ("Fill Edge Softness",   Range(0,0.08)) = 0.02
        _MeshMinY        ("Mesh Min Y",           Float) = -0.5
        _MeshMaxY        ("Mesh Max Y",           Float) = 0.5

        // View-mode color override (used by Thermal / Balancing views)
        _BaseColor       ("Override Color",       Color)  = (0,0,0,0)
        _RimIntensity    ("Rim Intensity",        Range(0,3)) = 1.0
        _ColorOverride   ("Color Override Blend", Range(0,1)) = 0.0

        // Glass properties
        _GlassColor      ("Glass Tint",           Color)  = (0.7, 0.95, 0.7, 0.08)
        _GlassRoughness  ("Glass Roughness",      Range(0,1)) = 0.05
        _FresnelPower    ("Fresnel Power",        Range(1,8)) = 4.0
        _FresnelStrength ("Fresnel Strength",     Range(0,2)) = 1.2

        // Liquid properties
        _LiquidOpacity   ("Liquid Opacity",       Range(0,1)) = 0.85
        _LiquidGlow      ("Liquid Glow",          Range(0,3)) = 1.4
        _LiquidShininess ("Liquid Shininess",     Range(0,1)) = 0.7
        _WaveSpeed       ("Wave Speed",           Range(0,3)) = 1.2
        _WaveStrength    ("Wave Strength",        Range(0,0.05)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
        }

        // ── Pass 1: Liquid fill (back faces first for correct transparency) ──
        Pass
        {
            Name "LiquidFill"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment fragLiquid

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ChargeColor;
                half4 _WarnColor;
                half4 _LowColor;
                half  _FillLevel;
                half  _WarnThreshold;
                half  _LowThreshold;
                half  _FillSoftness;
                half  _MeshMinY;
                half  _MeshMaxY;
                half4 _BaseColor;
                half  _RimIntensity;
                half  _ColorOverride;
                half4 _GlassColor;
                half  _GlassRoughness;
                half  _FresnelPower;
                half  _FresnelStrength;
                half  _LiquidOpacity;
                half  _LiquidGlow;
                half  _LiquidShininess;
                half  _WaveSpeed;
                half  _WaveStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  localY      : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.positionWS  = posInputs.positionWS;
                OUT.localY      = (IN.positionOS.y - _MeshMinY) / (_MeshMaxY - _MeshMinY);
                return OUT;
            }

            half4 GetLiquidColor()
            {
                half4 normalColor;
                if (_FillLevel < _LowThreshold)
                {
                    normalColor = _LowColor;
                }
                else if (_FillLevel < _WarnThreshold)
                {
                    half t = (_FillLevel - _LowThreshold) / (_WarnThreshold - _LowThreshold);
                    normalColor = lerp(_LowColor, _WarnColor, t);
                }
                else
                {
                    half t = (_FillLevel - _WarnThreshold) / (1.0 - _WarnThreshold);
                    normalColor = lerp(_WarnColor, _ChargeColor, t);
                }

                // Thermal / Balancing views drive _BaseColor + _ColorOverride to
                // replace the charge-level color with a status color.
                return lerp(normalColor, _BaseColor, _ColorOverride);
            }

            half4 fragLiquid(Varyings IN) : SV_Target
            {
                // Animate the fill surface with a gentle wave
                float wave = sin(IN.positionWS.x * 8.0 + _Time.y * _WaveSpeed) *
                             cos(IN.positionWS.z * 8.0 + _Time.y * _WaveSpeed * 0.7)
                             * _WaveStrength;

                float fillLine = _FillLevel + wave;

                // Only render liquid portion (below fill line)
                half fillMask = 1.0 - smoothstep(
                    fillLine - _FillSoftness,
                    fillLine + _FillSoftness,
                    IN.localY);

                if (fillMask < 0.01) discard;

                half4 liquidCol = GetLiquidColor();

                // Lighting on liquid
                half3 normalWS   = normalize(IN.normalWS);
                Light mainLight  = GetMainLight();
                half  NdotL      = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse    = liquidCol.rgb * (NdotL * 0.6 + 0.4);

                // Specular highlight on liquid surface
                half3 viewDir    = normalize(IN.viewDirWS);
                half3 halfDir    = normalize(mainLight.direction + viewDir);
                half  spec       = pow(saturate(dot(normalWS, halfDir)), 32.0) * _LiquidShininess * _RimIntensity;

                // Glow from within — boosted by _RimIntensity (thermal/balancing pulse)
                half3 glow       = liquidCol.rgb * _LiquidGlow * 0.3 * _RimIntensity;

                half3 finalColor = diffuse + spec + glow;
                half  alpha      = fillMask * _LiquidOpacity;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // ── Pass 2: Glass shell (front faces, renders over liquid) ──────────
        Pass
        {
            Name "GlassShell"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment fragGlass

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ChargeColor;
                half4 _WarnColor;
                half4 _LowColor;
                half  _FillLevel;
                half  _WarnThreshold;
                half  _LowThreshold;
                half  _FillSoftness;
                half  _MeshMinY;
                half  _MeshMaxY;
                half4 _BaseColor;
                half  _RimIntensity;
                half  _ColorOverride;
                half4 _GlassColor;
                half  _GlassRoughness;
                half  _FresnelPower;
                half  _FresnelStrength;
                half  _LiquidOpacity;
                half  _LiquidGlow;
                half  _LiquidShininess;
                half  _WaveSpeed;
                half  _WaveStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float  localY      : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.localY      = (IN.positionOS.y - _MeshMinY) / (_MeshMaxY - _MeshMinY);
                return OUT;
            }

            half4 GetLiquidColor()
            {
                half4 normalColor;
                if (_FillLevel < _LowThreshold)
                {
                    normalColor = _LowColor;
                }
                else if (_FillLevel < _WarnThreshold)
                {
                    half t = (_FillLevel - _LowThreshold) / (_WarnThreshold - _LowThreshold);
                    normalColor = lerp(_LowColor, _WarnColor, t);
                }
                else
                {
                    half t = (_FillLevel - _WarnThreshold) / (1.0 - _WarnThreshold);
                    normalColor = lerp(_WarnColor, _ChargeColor, t);
                }

                return lerp(normalColor, _BaseColor, _ColorOverride);
            }

            half4 fragGlass(Varyings IN) : SV_Target
            {
                half3 normalWS  = normalize(IN.normalWS);
                half3 viewDir   = normalize(IN.viewDirWS);
                Light mainLight = GetMainLight();

                // ── Fresnel rim — core of glass look ──────────────
                half NdotV   = saturate(dot(normalWS, viewDir));
                half fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelStrength * _RimIntensity;

                // ── Specular reflection ───────────────────────────
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half  NdotH   = saturate(dot(normalWS, halfDir));
                half  spec    = pow(NdotH, 128.0) * (1.0 - _GlassRoughness) * _RimIntensity;

                // ── Subtle tint from liquid color (glass is tinted) ──
                half4 liquidCol  = GetLiquidColor();
                half3 glassTint  = lerp(_GlassColor.rgb, liquidCol.rgb * 0.3, 0.4);

                // When a status override is active (thermal/balancing), tint the
                // glass shell toward the status color so the whole cell reads clearly.
                glassTint += liquidCol.rgb * _ColorOverride * 0.35;

                // ── Thin subsurface hint in filled area ───────────
                half inLiquid    = 1.0 - smoothstep(
                    _FillLevel - 0.05,
                    _FillLevel + 0.05,
                    IN.localY);
                glassTint       += liquidCol.rgb * inLiquid * 0.08;

                half3 finalColor = glassTint + spec * 0.9 + fresnel * 0.4;

                // Glass is mostly transparent — only rim and specular are visible
                half alpha = _GlassColor.a + fresnel * 0.5 + spec * 0.4 + _ColorOverride * 0.25;
                alpha      = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}