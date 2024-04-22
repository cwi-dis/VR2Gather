
Shader "cwipc/PointCloudUniform"{
	Properties{
		_Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
		_PointSize("Point Size", Float) = 0.05
		_PointSizeFactor("Point Size multiply", Float) = 1.0
		_OverridePointSize("Override Point Size", Float) = 0.0
	}
	
	SubShader {
		// This shader does not use the geometry engine, it renders squares for each point.
		Lighting Off
		LOD 100
		Cull Off
		Tags {
			"Queue" = "AlphaTest" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent"
		}

		Pass {
			// Pass two: normal sized points with no transparency
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
/* Second pass does not improve quality. Need to investigate. xxxjack
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
                //
                // xxxjack I think this computation is wrong. Undoutedly I can get the
                // correct information from the various matrices but I don't know how.
                //
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
*/
	}
}
