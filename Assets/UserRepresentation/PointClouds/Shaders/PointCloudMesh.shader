Shader "Entropy/PointCloudMesh"{
    Properties{
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
        _MainTex("Texture", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader{
        Lighting Off
        LOD 100
        Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}        LOD 200

        Pass {
        CGPROGRAM
                #pragma target 4.0
                #pragma vertex Vertex
                #pragma geometry Geometry
                #pragma fragment Fragment
                #pragma multi_compile _UNITY_COLORSPACE_GAMMA

                #include "UnityCG.cginc"
                

                half4       _Tint;
                half        _PointSize;
                sampler2D	_MainTex;
                fixed		_Cutoff;

                struct appdata
                {
                    float4  vertex : POSITION;
                    half3   color : COLOR;
                };

                struct v2f {
                    float4  position : SV_Position;
                    half3   color : COLOR;
                    half2	uv : TEXCOORD0;
//					float4  size : PSIZE;
                };

                v2f Vertex(appdata v) {
                    v2f o;
                    o.position = UnityObjectToClipPos(v.vertex);

                    half3 col = v.color.rgb;
                    col *= _Tint.rgb * 2;
                    o.color = col;
//					o.size = (_PointSize*50) * o.position.w; // 50->Magic number to fatten pixels.
                    return o;
                }

                [maxvertexcount(4)]
                void Geometry(point v2f input[1], inout TriangleStream<v2f> outStream) {
                    float4 origin = input[0].position;
                    float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);
#if SHADER_API_GLCORE || SHADER_API_METAL
                    extent.x *= -1;
#endif
                    // Copy the basic information.
                    v2f o = input[0];
                    o.position.xzw = origin.xzw;

                    // Bottom-Left side vertex
                    o.position.x = origin.x + extent.x;
                    o.position.y = origin.y + extent.y;
                    o.uv = half2(1, 1);
                    outStream.Append(o);

                    // Up-Left vertex
                    o.position.x = origin.x - extent.x;
                    o.position.y = origin.y + extent.y;
                    o.uv = half2(0, 1);
                    outStream.Append(o);

                    // Up-Right side vertex
                    o.position.x = origin.x + extent.x;
                    o.position.y = origin.y - extent.y;
                    o.uv = half2(1, 0);
                    outStream.Append(o);

                    // Bottom-Right vertex
                    o.position.x = origin.x - extent.x;
                    o.position.y = origin.y - extent.y;
                    o.uv = half2(0, 0);
                    outStream.Append(o);

                    outStream.RestartStrip();
                }

      
                half4 Fragment(v2f input) : SV_Target{
                    half a = tex2D(_MainTex, input.uv).r;
                    clip(a - _Cutoff);
                    half4 c = half4(input.color, _Tint.a) * a;

                    return c;
                }
                ENDCG
            }
    }
}