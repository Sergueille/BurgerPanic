Shader "Unlit/Stripes"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _Size ("Size", float) = 1
        _PixelSize ("Pixel Size", float) = 1
        _ColorA ("Color A", color) = (1, 1, 1, 1)
        _ColorB ("Color B", color) = (1, 0.5, 0.5, 1)
        _Threshold ("Threshold", float) = 0.5
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Size;
            float _PixelSize;
            float4 _ColorA;
            float4 _ColorB;
            float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int2 px = int2(round(i.uv.x * _ScreenParams.x / _ScreenParams.y / _PixelSize),  round(i.uv.y / _PixelSize));

                if ((px.x + px.y) % _Size > _Size * _Threshold)
                    return _ColorA;
                else
                    return _ColorB;
            }
            ENDCG
        }
    }
}
