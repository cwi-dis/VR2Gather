Shader "Entropy/PointCloud40"{
    Properties{
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass {
        CGPROGRAM
                #pragma target 4.0
                #pragma vertex Vertex

                #pragma fragment Fragment
                #pragma multi_compile _UNITY_COLORSPACE_GAMMA

                #include "UnityCG.cginc"
                

                half4       _Tint;
                half        _PointSize;

                struct appdata
                {
                    float4  vertex : POSITION;
                    half3   color : COLOR;
                };

                struct v2f {
                    float4  position : SV_Position;
                    half3   color : COLOR;
					float   size : PSIZE;
                };

                v2f Vertex(appdata v) {
                    v2f o;
                    o.position = UnityObjectToClipPos(v.vertex);

                    half3 col = v.color.rgb;
                    col *= _Tint.rgb * 2;
                    o.color = col;
					o.size = (_PointSize*50) * o.position.w; // 50->Magic number to fatten pixels.
                    return o;
                }


      
                half4 Fragment(v2f input) : SV_Target{
                    return half4(input.color,1);
                }
                ENDCG
            }
    }
}