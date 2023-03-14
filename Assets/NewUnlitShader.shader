Shader "Custom/Gradient" 
{
    Properties
    { 
        _MainTex("Texture", 2D) = "white" {}
        _Direction("Direction", Range(0, 360)) = 0
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (0,0,0,1) 
    }
    SubShader
    {
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

            struct v2f { float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION; };
            float4x4 _ObjectToWorld;
            float4x4 _WorldToObject;
            float _Direction;
            float4 _Color1;
            float4 _Color2;
            sampler2D _MainTex;
           
            v2f vert(appdata v) { 
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o; 
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                float2 uv = i.uv;
                float4 color1 = _Color1;
                float4 color2 = _Color2;
                float2 dir = normalize(float2(cos(_Direction), sin(_Direction)));
                float4 gradient = lerp(color1, color2, dot(dir, uv - 0.5) + 0.5);
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return texColor * gradient;
            }

        ENDCG
        }
    }
    FallBack "Diffuse"
}