Shader "UI/Vein Vanguard Arena"
{
    Properties
    {
        [PerRendererData] _MainTex ("Artwork", 2D) = "white" {}
        _ArenaPan ("Far layer camera compensation", Vector) = (0,0,0,0)
        _ArenaMotion ("Pulse, ground height, stage", Vector) = (0,0,0.47,0)
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _ClipRect, _ArenaPan, _ArenaMotion;

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
                output.color = input.color;
                output.uv = input.uv;
                output.canvasPosition = input.position.xy;
                return output;
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                float2 uv = input.uv;
                float ground = _ArenaMotion.z;
                float walls = smoothstep(ground + .05, ground + .38, uv.y);
                float edges = smoothstep(0, .08, uv.x) * smoothstep(0, .08, 1 - uv.x);
                float2 pulse = float2((uv.x - .5) * .004, -.003) * _ArenaMotion.y;
                uv += (_ArenaPan.xy + pulse) * walls * edges;
                float floorFlow = 1 - smoothstep(ground - .20, ground - .06, uv.y);
                uv.x += .0016 * _ArenaMotion.x * floorFlow * edges;
                fixed4 color = tex2D(_MainTex, saturate(uv)) * input.color;

                // Soften busy details near the fighters while retaining the original palette.
                float quiet = 1 - smoothstep(.07, .25, abs(uv.x - .29));
                quiet = max(quiet, .60 * (1 - smoothstep(.05, .22, abs(uv.x - .76))));
                quiet *= 1 - smoothstep(.05, .29, abs(uv.y - (ground + .06)));
                float treatment = lerp(.25, .12, _ArenaMotion.w) * quiet;
                half value = dot(color.rgb, half3(.2126, .7152, .0722));
                color.rgb = lerp(color.rgb, value, treatment) * (.96 - treatment * .28);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.canvasPosition, _ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
