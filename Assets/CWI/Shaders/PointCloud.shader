
Shader "Entropy/PointCloud"{
	Properties{
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 100

		Pass {
				Tags { "LightMode" = "ForwardBase" }
				CGPROGRAM

				#pragma target 4.0
				#pragma vertex Vertex
				#pragma geometry Geometry
				#pragma fragment Fragment

				#pragma multi_compile_fog
				#pragma multi_compile _ UNITY_COLORSPACE_GAMMA

				#include "UnityCG.cginc"

				#define PCX_MAX_BRIGHTNESS 16

				half3 PcxDecodeColor(uint data) {
					half r = (data >> 0) & 0xff;
					half g = (data >> 8) & 0xff;
					half b = (data >> 16) & 0xff;
					return half3(r, g, b) / 255;
				}

				struct Varyings {
					float4	position : SV_Position;
					half3	color : COLOR;
					UNITY_FOG_COORDS(0)
				};

				half4		_Tint;
				float4x4	_Transform;
				half		_PointSize;

				StructuredBuffer<float4> _PointBuffer;

				Varyings Vertex(uint vid : SV_VertexID) {
					float4 pt = _PointBuffer[vid];
					float4 pos = mul(_Transform, float4(pt.xyz, 1));
					half3  col = PcxDecodeColor(asuint(pt.w));

					#ifdef UNITY_COLORSPACE_GAMMA
						col *= _Tint.rgb * 2;
					#else
						col *= LinearToGammaSpace(_Tint.rgb) * 2;
						col = GammaToLinearSpace(col);
					#endif

					Varyings o;
					o.position = UnityObjectToClipPos(pos);
					o.color = col;
					UNITY_TRANSFER_FOG(o, o.position);
					return o;
				}

				[maxvertexcount(4)]
				void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream) {
					float4 origin = input[0].position;
					float2 extent = abs(UNITY_MATRIX_P._11_22  * _PointSize);
#if SHADER_API_GLCORE	 
					extent.x *= -1;
#endif
					// Copy the basic information.
					Varyings o = input[0];
					o.position.xzw = origin.xzw;

					// Bottom side vertex
					o.position.x = origin.x;
					o.position.y = origin.y + extent.y;
					outStream.Append(o);

					// Left vertex
					o.position.x = origin.x - extent.x;
					o.position.y = origin.y;
					outStream.Append(o);

					// Right side vertex
					o.position.x = origin.x + extent.x;
					o.position.y = origin.y;
					outStream.Append(o);

					// Top vertex
					o.position.x = origin.x;
					o.position.y = origin.y - extent.y;
					outStream.Append(o);

					outStream.RestartStrip();
				}

				half4 Fragment(Varyings input) : SV_Target {
					half4 c = half4(input.color, _Tint.a);
					UNITY_APPLY_FOG(input.fogCoord, c);
					return c;
				}


				ENDCG
			}
	}
}
/*
Shader "Entropy/PointCloud"{
	Properties {
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Pass {
			CGPROGRAM

			#pragma target 4.5
			#pragma vertex Vertex
			#pragma fragment Fragment

			#pragma multi_compile_fog
			#pragma multi_compile _ UNITY_COLORSPACE_GAMMA

			#include "UnityCG.cginc"

			#define PCX_MAX_BRIGHTNESS 16

			half3 PcxDecodeColor(uint data) {
				half r = (data >>  0) & 0xff;
				half g = (data >>  8) & 0xff;
				half b = (data >> 16) & 0xff;
				return half3(r, g, b) / 255;
			}

			struct Varyings {
				float4 position : SV_Position;
				half3 color : COLOR;
				UNITY_FOG_COORDS(0)
			};

			half4		_Tint;
			float4x4	_Transform;
			half		_PointSize;

			StructuredBuffer<float4> _PointBuffer;

			Varyings Vertex(uint vid : SV_VertexID) {
			float4 pt = _PointBuffer[vid];
			float4 pos = mul(_Transform, float4(pt.xyz, 1));
			half3  col = PcxDecodeColor(asuint(pt.w));

			#ifdef UNITY_COLORSPACE_GAMMA
				col *= _Tint.rgb * 2;
			#else
				col *= LinearToGammaSpace(_Tint.rgb) * 2;
				col = GammaToLinearSpace(col);
			#endif

				Varyings o;
				o.position = UnityObjectToClipPos(pos);
				o.color = col;
				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			half4 Fragment(Varyings input) : SV_Target {
				half4 c = half4(input.color, _Tint.a);
				UNITY_APPLY_FOG(input.fogCoord, c);
				return c;
			}


			ENDCG
		}
	}
}
*/