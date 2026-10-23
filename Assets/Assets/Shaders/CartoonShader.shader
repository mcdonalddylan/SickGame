Shader "Custom/CartoonShader"
{
    Properties
    {
        _BaseMap    ("Base Texture", 2D) = "white" {}
        _BaseColor  ("Base Color",   Color) = (1,1,1,1)
        _ShadeColor ("Shade Color",  Color) = (0.3,0.3,0.3,1)
        _ShadeColorOpacity ("Shade Color Opacity", Range(0,1)) = 1.0
        _ShadeSoftness     ("Shade Edge Softness", Range(0,0.5)) = 0.015
        _HighlightColor    ("Highlight Color",  Color) = (1,1,1,1)
        _HighlightThreshold("Highlight Threshold", Range(0,1)) = 0.8
        _HighlightOpacity  ("Highlight Opacity", Range(0,1)) = 0.3
        _HighlightSoftness ("Highlight Edge Softness", Range(0,0.5)) = 0.015
        _Threshold  ("Shade Threshold", Range(0,1)) = 0.5
        _OutlineColor    ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0.001,0.5)) = 0.015
        _WiggleAmount    ("Wiggle Amount", Range(0,0.05)) = 0.008
        _WiggleFPS       ("Wiggle FPS", Range(1,24)) = 6
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }
        LOD 200

        // ── PASS 1: Main toon shading ──────────────────────────────
        Pass
        {
            Name "ToonPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseColor;
            float4 _ShadeColor;
            float  _ShadeColorOpacity;
            float  _ShadeSoftness;
            float4 _HighlightColor;
            float  _HighlightThreshold;
            float  _HighlightOpacity;
            float  _HighlightSoftness;
            float  _Threshold;

            // Vertex stage: transforms the vertex to clip space and passes the world-space
            // normal and position on for lighting and shadow lookups in the fragment stage
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                o.uv          = v.uv;
                return o;
            }

            // Fragment stage: picks shade or base color from the main light (including received
            // shadows), then layers highlights from all lights on top
            half4 frag(Varyings i) : SV_Target
            {
                float3 normal   = normalize(i.normalWS);
                float4 tex      = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float4 baseResult  = _BaseColor * tex;
                float4 adjustedShadeColor = lerp(_BaseColor, _ShadeColor, _ShadeColorOpacity) * tex;

                // Main light: the shadow coord lets GetMainLight sample the shadow map, so shadows
                // cast by other objects onto this one are included in shadowAttenuation
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float  NdotL    = dot(normal, lightDir);

                // Soft shade transition using smoothstep; received shadows push the surface into shade
                float shadeTransition = smoothstep(_Threshold - _ShadeSoftness, _Threshold + _ShadeSoftness, NdotL);
                shadeTransition *= mainLight.shadowAttenuation;
                float4 color = lerp(adjustedShadeColor, baseResult, shadeTransition);

                // Highlights from all lights with soft transition
                float highlight = 0.0;

                // Main light highlight with soft edges
                float mainLightHighlight = smoothstep(_HighlightThreshold - _HighlightSoftness, _HighlightThreshold + _HighlightSoftness, NdotL) * mainLight.shadowAttenuation;
                highlight += mainLightHighlight;

                // Additional lights (point/spot): each adds a highlight scaled by distance and shadows
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                    float3 additionalLightDir = normalize(light.direction);
                    float additionalNdotL = dot(normal, additionalLightDir);
                    float additionalHighlight = smoothstep(_HighlightThreshold - _HighlightSoftness, _HighlightThreshold + _HighlightSoftness, additionalNdotL) * light.distanceAttenuation * light.shadowAttenuation;
                    highlight += additionalHighlight;
                }

                // Clamp the combined highlight to 0-1, then blend the highlight color over the result
                highlight = min(highlight, 1.0);
                color = lerp(color, _HighlightColor * tex, highlight * _HighlightOpacity);

                return color;
            }
            ENDHLSL
        }

        // ── PASS 2: Wiggling inverted-hull outline ─────────────────
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

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
                float4 positionHCS : SV_POSITION;
            };

            // Outline pass: draws an enlarged copy of the mesh with front faces culled, so only
            // the back faces show around the silhouette as an outline
            float  _OutlineThickness;
            float  _WiggleAmount;
            float  _WiggleFPS;
            float4 _OutlineColor;

            // Cheap pseudo-random value in 0-1 from a single float
            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            // Vertex stage: pushes each vertex outward along its normal by the thickness plus a
            // random wobble that only changes _WiggleFPS times per second
            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);

                float t = floor(_Time.y * _WiggleFPS) / _WiggleFPS;

                float wobble = (Hash(dot(v.positionOS.xyz, float3(12.9898,78.233,37.719)) + t) - 0.5) * 2.0;

                posWS += normalWS * (_OutlineThickness + wobble * _WiggleAmount);

                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            // Fragment stage: flat outline color
            half4 frag(Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ── PASS 3: Depth-only (for _CameraDepthTexture) ──────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // Vertex stage: only the clip-space position is needed for depth
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            // Fragment stage: no color output (ColorMask 0); depth is written by ZWrite
            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ── PASS 4: Depth + Normals (for SSAO) ─────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

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
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            // Vertex stage: outputs clip-space position and world-space normal
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // Fragment stage: encodes the world normal into 0-1 color for the normals texture (SSAO)
            half4 frag(Varyings i) : SV_Target
            {
                return half4(normalize(i.normalWS) * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }

        // ── PASS 5: Shadow Caster (for casting shadows) ─────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 2.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // Computes the clip-space position as seen from the light, with shadow bias applied
            // along the normal to avoid shadow acne, and depth clamped to the near plane
            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));

#if UNITY_REVERSED_Z
                positionHCS.z = min(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
#else
                positionHCS.z = max(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

                return positionHCS;
            }

            // Vertex stage: positions the mesh in the shadow map
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = GetShadowPositionHClip(input);
                return output;
            }

            // Fragment stage: no color needed, only depth is stored in the shadow map
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
