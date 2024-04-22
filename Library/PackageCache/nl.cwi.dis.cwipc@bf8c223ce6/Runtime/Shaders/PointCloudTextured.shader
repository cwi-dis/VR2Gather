//
// Jack has documented this shader as he slowly started to understand it.
// Hope it helps someon, please fix any errors and add any unseful insights.
//
// The shader language is called HLSL. Details on the Unity implementation are
// in https://docs.unity3d.com/Manual/SL-ShaderSemantics.html
//

Shader "cwipc/PointCloudTextured"{
	//
	// The Properties block declares which variables of the shader can been seen on the CPU
	// side, through the MaterialPropertyBlock structure.
	//
	// The names recur below, in variable declarations. The mapping between the types here and the
	// types below can also be found somewhere in the Unity manual (please add link if you find it).
	//
	Properties {
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
		_PointSizeFactor("Point Size multiply", Float) = 1.0
		_MainTex("Texture", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
		_OverridePointSize("Override Point Size", Float) = 0.0
	}

	//
	// Each shader can consist of a number of subshaders. Only one of the subshaders will be used:
	// the first one that uses only features that are supported on the current hardware. If
	// none of the subshaders is usable there is a fallback mechanism (see the Unity documentation).
	//
	SubShader {
		//
		// This subshader requires the geometry engine,
		// It uses the geometry step to convert points to rectangles, and then uses the texture to
		// sample the points off.
		//

		//
		// Here follow some parameters that define how this subshader operates.
		//
		Lighting Off
		LOD 100
		Cull Off
		//
		// xxxjack We could try enabling/disabling alpha blending to see whether it afects performance.
		// xxxjack It doesn't affect performance (at least not on a RTX2080), but enabling alpha blending does
		// make things more ugly.
		//Blend SrcAlpha OneMinusSrcAlpha
		//BlendOp Add
		Tags {
			"Queue" = "AlphaTest" 
			"IgnoreProjector" = "True" 
			"RenderType" = "TransparentCutout"
		}

		//
		// Each subshader can do multiple passes (one after the other).
		//
		Pass {
			//
			// Passes, like subshaders, have some parameters to govern how they operate.
			//
			Name "PointCloudBufferAsTexture"
			Tags { 
				"LightMode" = "ForwardBase" 
			}
			//
			// Finally: code for the GPU. The bit between CGPROGRAM and CGEND is compiled
			// for the GPU.
			//
			CGPROGRAM

			//
			// These pragmas tell what features this program needs from the GPU, what type of things
			// it can render, and which steps it uses.
			//
			// target specifies GPU capabilities in a very high level way. See Unity documentation.
			// Bigger numbers are newer GPUs.
			#pragma target 5.0
			// These next three pragmas explain we have a vertex step (called Vertex, below),
			// a geometry step (Geometry) and a fragment step (Fragment).
			#pragma vertex Vertex
			#pragma geometry Geometry
			#pragma fragment Fragment

			#include "UnityCG.cginc"

			//
			// A helper function to turn a color from a 32-bit RGBx word into three half-floats,
			// which is the format the GPU seems to want.
			//
			half3 PcxDecodeColor(uint data) {
				half r = (data >> 0) & 0xff;
				half g = (data >> 8) & 0xff;
				half b = (data >> 16) & 0xff;
				return half3(r, g, b) / 255;
			}

			//
			// This structure declaration is used as the "return value" from the vertex step (Vertex, below).
			// It is the information that is passed on to the next step (the geometry step Geometry).
			//
			struct Varyings {
				float4	position : SV_Position;
				half4	color : COLOR;
				half2	uv : TEXCOORD0;
//					UNITY_FOG_COORDS(0)
			};

			//
			// Here are the "global variable" declarations that match the properties above.
			//
			half4		_Tint;
			float4x4	_Transform;
			half		_PointSize;
			half		_PointSizeFactor;
			sampler2D	_MainTex;
			fixed		_Cutoff;
			half		_OverridePointSize;

			// This structured buffer contains the actual pointcloud data, 16 bytes per point.
			// The first 3 floats are indeed floats (x, y, z), the fourth 32 bit word is actually
			// RGBx, which is cast to a uint and then fed to the PcxDecodeColor function (above)
			// to get the colors.
			//
			// The buffer is passed from CPU to GPU through the MaterialPropertyBlock, just like
			// the other global variables.
			//
			StructuredBuffer<float4> _PointBuffer;

			//
			// First step: the vertex shader. There is a lot of magic in this declaration. The
			// colon-notation is used (I think) to communicate the semantics of this step implementation
			// to whatever calls it. The ": SV_VertexID" signals that we want only vertex indices
			// (which we will then use to get the actual per-point data from the _PointBuffer).
			//
			Varyings Vertex(uint vid : SV_VertexID) {
				float4 pt = _PointBuffer[vid];
				//
				// Two bits of magic in the following line:
				// - pt.xyz will take the first three floats of a float4 and return them as a float3
				// - float4(float3, float) will create a float4 from a float3 and a float.
				//
				// I think the xyz works for any letter combination (and order) of x, y, z, w (and similarly for
				// colors for r, g, b, a) but I'm not 100% sure.'
				//
				float4 pos = mul(_Transform, float4(pt.xyz, 1));
				half4  col = half4(PcxDecodeColor(asuint(pt.w)), _Tint.a);

#if UNITY_COLORSPACE_GAMMA
				col.rgb *= _Tint.rgb * 2;
#else
				col.rgb *= LinearToGammaSpace(_Tint) * 2;
				col.rgb = GammaToLinearSpace(col);
#endif
				Varyings o;
				o.position = UnityObjectToClipPos(pos);
				o.color = col;
				o.uv = half2(0.5, 0.5);
//					UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			//
			// Second step: the geometry shader. I have not been able to find any information
			// on how the magic of this declaration works.
			//
			// Apparently it gets one Varyings from the vertex step and produces a couple of
			// Varyings in the output parameter outStream. 
			//
			[maxvertexcount(4)]
			void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream) {
				float4 origin = input[0].position;
				float2 extent = abs(UNITY_MATRIX_P._11_22  * _PointSize * _PointSizeFactor);
				if (_OverridePointSize != 0) {
					extent = abs(UNITY_MATRIX_P._11_22 * _OverridePointSize);
				}
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

			//
			// Last step: the fragment shader. Something goes over each point in the triangles
			// produced by the geometry shader and calls the fragment shader for each point.
			// The fragment step now determines the  color (based on the texture and the u,v texture
			// coordinates of the point)
			//
			half4 Fragment(Varyings input) : SV_Target{
				half4 c = input.color;
				half4 tc = tex2D(_MainTex, input.uv);
				c.a *= tc.a;
				clip(c.a - _Cutoff);
				return c;
			}


			ENDCG
		}
	}
	
	SubShader {
		// This shader does not use the geometry engine, but does two passes.
		Lighting Off
		LOD 100
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		Tags {
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent"
		}

		Pass {
			// Pass one: normal sized points with no transparency
			Name "PointCloudBufferAsPoints"
			Tags { 
				"LightMode" = "ForwardBase" 
			}
			CGPROGRAM

			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "UnityCG.cginc"

			half3 PcxDecodeColor(uint data) {
				half r = (data >> 0) & 0xff;
				half g = (data >> 8) & 0xff;
				half b = (data >> 16) & 0xff;
				return half3(r, g, b) / 255;
			}

			struct Varyings {
				float4	position : SV_Position;
				half4	color : COLOR;
				half  size : PSIZE;
//					UNITY_FOG_COORDS(0)
			};

			half4		_Tint;
			float4x4	_Transform;
			half		_PointSize;
			half		_PointSizeFactor;
			sampler2D	_MainTex;
			fixed		_Cutoff;
			half		_OverridePointSize;

			StructuredBuffer<float4> _PointBuffer;

			Varyings Vertex(uint vid : SV_VertexID) {
				float4 pt = _PointBuffer[vid];
				float4 pos = mul(_Transform, float4(pt.xyz, 1));
				half4  col = half4(PcxDecodeColor(asuint(pt.w)), _Tint.a);

#if UNITY_COLORSPACE_GAMMA
				col.rgb *= _Tint.rgb * 2;
#else
				col.rgb *= LinearToGammaSpace(_Tint) * 2;
				col.rgb = GammaToLinearSpace(col);
#endif
				Varyings o;
				o.position = UnityObjectToClipPos(pos);
				o.color = col;
                float pixelsPerMeter = _ScreenParams.y / o.position.w;
				if (_OverridePointSize == 0) {
					float pixelsPerMeter = _ScreenParams.y / o.position.w;
					o.size = _PointSize * _PointSizeFactor * pixelsPerMeter;
				}
				else
				{
					o.size = _OverridePointSize;
				}
//				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			half4 Fragment(Varyings input) : SV_Target{
                half4 c = input.color;
                return c;
			}
			ENDCG
		}
/*
#if XXXJACK_DOES_NOT_WORK
		Pass {
			// Pass two: double-sized points with transparency
			Tags { 
				"LightMode" = "ForwardBase" 
			}
			CGPROGRAM

			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "UnityCG.cginc"

			half3 PcxDecodeColor(uint data) {
				half r = (data >> 0) & 0xff;
				half g = (data >> 8) & 0xff;
				half b = (data >> 16) & 0xff;
				return half3(r, g, b) / 255;
			}

			struct Varyings {
				float4	position : SV_Position;
				half4	color : COLOR;
				float  size : PSIZE;
//					UNITY_FOG_COORDS(0)
			};

			half4		_Tint;
			float4x4	_Transform;
			half		_PointSize;
			half		_PointSizeFactor;
			sampler2D	_MainTex;
			fixed		_Cutoff;

			StructuredBuffer<float4> _PointBuffer;

			Varyings Vertex(uint vid : SV_VertexID) {
				float4 pt = _PointBuffer[vid];
				float4 pos = mul(_Transform, float4(pt.xyz, 1));
				half4  col = half4(PcxDecodeColor(asuint(pt.w)), _Tint.a*0.5);

#if UNITY_COLORSPACE_GAMMA
				col.rgb *= _Tint.rgb * 2;
#else
				col.rgb *= LinearToGammaSpace(_Tint) * 2;
				col.rgb = GammaToLinearSpace(col);
#endif
				Varyings o;
				o.position = UnityObjectToClipPos(pos);
				o.color = col;
           
                float pixelsPerMeter = _ScreenParams.y / o.position.w;
                o.size = _PointSize * _PointSizeFactor * pixelsPerMeter * 3;
//					UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			half4 Fragment(Varyings input) : SV_Target{
                half4 c = input.color;
                return c;
			}
			ENDCG
		}
#endif
*/
	}
}
