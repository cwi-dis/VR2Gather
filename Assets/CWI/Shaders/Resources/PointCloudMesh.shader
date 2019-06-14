Shader "Entropy/PointCloud40"{
	Properties{
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
	}
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass {
		Tags { "LightMode" = "ForwardBase" }
		
		CGPROGRAM
				#pragma multi_compile_fwdbase

				#include "AutoLight.cginc"

				#pragma target 4.0
				#pragma vertex Vertex

				#pragma geometry Geometry
				#pragma fragment Fragment
				#pragma multi_compile _UNITY_COLORSPACE_GAMMA

				#include "UnityCG.cginc"

				half4		_Tint;
				half		_PointSize;

				struct appdata
				{
					float4  vertex : position;
					half3	color : COLOR;
				};

				struct v2f {
					float4	pos : SV_POSITION;
					half3	color : COLOR;

					LIGHTING_COORDS(0, 1)
				};

				v2f Vertex(appdata v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);

					half3 col = v.color.rgb;
					col *= _Tint.rgb * 2;
					o.color = col;

					TRANSFER_VERTEX_TO_FRAGMENT(o);

					return o;
				}

				[maxvertexcount(4)]
				void Geometry(point v2f input[1], inout TriangleStream<v2f> outStream) {
					float4 origin = input[0].pos;

					float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);
			#if SHADER_API_GLCORE || SHADER_API_METAL
					extent.x *= -1;
			#endif
					// Copy the basic information.
					v2f o = input[0];
					o.pos.xzw = origin.xzw;

					// Bottom side vertex
					o.pos.x = origin.x;
					o.pos.y = origin.y + extent.y;
					outStream.Append(o);

					// Left vertex
					o.pos.x = origin.x - extent.x;
					o.pos.y = origin.y;
					outStream.Append(o);

					// Right side vertex
					o.pos.x = origin.x + extent.x;
					o.pos.y = origin.y;
					outStream.Append(o);

					// Top vertex
					o.pos.x = origin.x;
					o.pos.y = origin.y - extent.y;
					outStream.Append(o);

					outStream.RestartStrip();
				}

				half4 Fragment(v2f input) : SV_Target{
					float attenuation = LIGHT_ATTENUATION(input);
					return half4(input.color,1);
				}
				ENDCG
			}
	}
	Fallback "VertexLit"
}