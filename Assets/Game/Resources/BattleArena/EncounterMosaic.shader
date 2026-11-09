Shader "UI/Vein Vanguard Encounter Mosaic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame", 2D) = "white" {}
        _Grid ("Screen block grid", Vector) = (1600, 900, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Grid;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 grid = _Grid.xy;
                float2 sampleUV = (floor(input.uv * grid) + 0.5) / grid;
                return fixed4(tex2D(_MainTex, sampleUV).rgb, 1);
            }
            ENDCG
        }
    }
}
