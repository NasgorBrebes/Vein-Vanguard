Shader "UI/Vein Vanguard Canyon V2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map artwork", 2D) = "white" {}
        [NoScaleOffset] _Motion ("Crown, tips and bridge motion", 2D) = "gray" {}
        [NoScaleOffset] _Regions ("Energy, mist, reflection and motion groups", 2D) = "white" {}
        _Sway0 ("Bridge and tree motion", Vector) = (0,0,0,0)
        _Sway1 ("Tree motion", Vector) = (0,0,0,0)
        _Tips0 ("Leaf tip motion", Vector) = (0,0,0,0)
        _Tips1 ("Leaf tip motion", Vector) = (0,0,0,0)
        _Lighting ("Phase, parallax and light pulse", Vector) = (0,0,0,0)
        _MistA ("Layer positions", Vector) = (0,0,0,0)
        _MistB ("Layer positions", Vector) = (0,0,0,0)
        _MistStrengthA ("Layer opacity", Vector) = (0,0,0,0)
        _MistStrengthB ("Layer opacity", Vector) = (0,0,0,0)
        _Glow ("Energy glow centers", Vector) = (0,0,0,0)
        _Flow ("Flow positions and intensity", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex, _Motion, _Regions;
            float4 _Regions_TexelSize, _ClipRect;
            float4 _Sway0, _Sway1, _Tips0, _Tips1, _Lighting;
            float4 _MistA, _MistB, _MistStrengthA, _MistStrengthB, _Glow, _Flow;
            static const float2 MapSize = float2(2048, 1448);

            struct Vertex
            {
                float4 position : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Fragment
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 canvasPosition : TEXCOORD1;
            };

            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                output.canvasPosition = input.position.xy;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half SoftEllipse(float2 p, float2 center, float2 radius)
            {
                float2 offset = (p - center) / radius;
                half falloff = saturate(1 - dot(offset, offset));
                return falloff * falloff;
            }

            half Mist(float2 p, float2 x, float2 strength, float y, float2 radius)
            {
                return SoftEllipse(p, float2(x.x, y), radius) * strength.x +
                       SoftEllipse(p, float2(x.y, y - 8), radius * float2(.77, .71)) * strength.y;
            }

            // Sample the row in artwork coordinates to keep the direction and curve of the Remotion light.
            half Filament(float2 p, float center, float intensity)
            {
                float x = p.x - center;
                half taper = saturate(1 - abs(x) / 350);
                float arc = x * x * .00023;
                half upper = saturate(1 - abs(p.y - (657 + arc - x * .045)) / 19);
                half lower = saturate(1 - abs(p.y - (746 + arc * .4 - x * .06)) / 15);
                return taper * intensity * (upper * upper + lower * lower * .71);
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                float2 uv = input.uv;
                float depth = 2 + 2.2 * smoothstep(.16, .30, 1 - uv.y)
                                - 7.4 * smoothstep(.40, .75, 1 - uv.y);
                float edge = smoothstep(0, .04, min(uv.x, 1 - uv.x));
                uv.x += depth * _Lighting.y * edge / MapSize.x;

                // Region IDs use exact texel centers. Coverage and displacement still filter smoothly.
                float2 nearest = (floor(uv * _Regions_TexelSize.zw) + .5) * _Regions_TexelSize.xy;
                float group = floor(tex2D(_Regions, nearest).a * 7.96875 + .5);
                float4 a = 1 - saturate(abs(group - float4(1, 2, 3, 4)));
                float4 b = 1 - saturate(abs(group - float4(5, 6, 7, -1)));
                float sway = dot(a, _Sway0) + dot(b, _Sway1);
                float tips = dot(a, _Tips0) + dot(b, _Tips1);
                float4 motion = tex2D(_Motion, uv) - (128.0 / 255);
                float2 move = 16 * (motion.rg * sway + motion.ba * tips);
                uv = saturate(uv + float2(move.x, -move.y) / MapSize);
                half3 region = tex2D(_Regions, uv).rgb;
                half3 artwork = tex2D(_MainTex, uv).rgb;

                float2 p = float2(uv.x, 1 - uv.y) * MapSize;
                half pulse = _Lighting.z;
                half energy = (
                    SoftEllipse(p, _Glow.xy, float2(381, 107)) * (.13 + .15 * pulse) +
                    SoftEllipse(p, _Glow.zw, float2(241, 70)) * .14 +
                    Filament(p, _Flow.x, _Flow.z) + Filament(p, _Flow.y, _Flow.w)
                ) * region.r;
                half reflected = (
                    SoftEllipse(p, float2(1170, 772), float2(512, 193)) * (.14 + .07 * pulse) +
                    SoftEllipse(p, float2(1090 + 57 * _Lighting.y, 520), float2(399, 104)) * .15 +
                    SoftEllipse(p, float2(758 - 73 * _Lighting.y, 885), float2(411, 150)) * .14
                ) * region.b;
                half haze = SoftEllipse(p, float2(1040 + 150 * _Lighting.y, 347), float2(790, 160))
                    * smoothstep(.69, .82, uv.y) * .027;
                half fog = (
                    Mist(p, _MistA.xy, _MistStrengthA.xy, 581, float2(338, 46)) +
                    Mist(p, _MistA.zw, _MistStrengthA.zw, 715, float2(269, 47)) +
                    Mist(p, _MistB.xy, _MistStrengthB.xy, 841, float2(296, 57)) +
                    Mist(p, _MistB.zw, _MistStrengthB.zw, 1054, float2(326, 69))
                ) * region.g;

                half3 color = lerp(artwork, half3(.87, .57, 1), saturate(energy + reflected + haze));
                color = lerp(color, half3(.78, .53, .94), saturate(fog));
                fixed4 result = fixed4(color, 1) * input.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.canvasPosition, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}

