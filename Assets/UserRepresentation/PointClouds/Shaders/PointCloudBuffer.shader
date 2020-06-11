
Shader "Entropy/PointCloud"{
	Properties{
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
		_MainTex("Texture", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}
		SubShader{
			Lighting Off
			LOD 100
			Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
	//				Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
	//				Blend SrcAlpha OneMinusSrcAlpha
	//				ZWrite Off

			Pass {
				Tags { "LightMode" = "ForwardBase" }
				CGPROGRAM

				#pragma target 5.0
				#pragma vertex Vertex
				#pragma geometry Geometry
				#pragma fragment Fragment

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
					half2	uv : TEXCOORD0;
//					UNITY_FOG_COORDS(0)
				};

				half4		_Tint;
				float4x4	_Transform;
				half		_PointSize;
				sampler2D	_MainTex;
				fixed		_Cutoff;

				StructuredBuffer<float4> _PointBuffer;

				Varyings Vertex(uint vid : SV_VertexID) {
					float4 pt = _PointBuffer[vid];
					float4 pos = mul(_Transform, float4(pt.xyz, 1));
					half3  col = PcxDecodeColor(asuint(pt.w));

					col *= _Tint.rgb * 2;

					Varyings o;
					o.position = UnityObjectToClipPos(pos);
					o.color = col;
//					UNITY_TRANSFER_FOG(o, o.position);
					return o;
				}

				[maxvertexcount(4)]
				void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream) {
					float4 origin = input[0].position;
					float2 extent = abs(UNITY_MATRIX_P._11_22  * _PointSize);
#if SHADER_API_GLCORE || SHADER_API_METAL
					extent.x *= -1;
#endif
					// Copy the basic information.
					Varyings o = input[0];
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

				half4 Fragment(Varyings input) : SV_Target{
					half a = tex2D(_MainTex, input.uv).r;
					clip(a - _Cutoff);
					half4 c = half4(input.color, _Tint.a) * a;
//					UNITY_APPLY_FOG(input.fogCoord, c);
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