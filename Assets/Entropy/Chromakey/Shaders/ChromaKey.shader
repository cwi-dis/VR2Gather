Shader "Unlit/Transparent Chroma" {


	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_thresh("Threshold", Range(0, 16)) = 0.8
		_slope("Slope", Range(0, 1)) = 0.2
		_keyingColor("Key Colour", Color) = (1,1,1,1)
	}

		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 100

			Lighting Off
			ZWrite Off
			AlphaTest Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass {
				CGPROGRAM
					#pragma vertex vert_img
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest

					sampler2D _MainTex;
					float3 _keyingColor;
					float _thresh; // 0.8
					float _slope; // 0.2

					#include "UnityCG.cginc"

				   float4 frag(v2f_img i) : COLOR{
				  float3 input_color = tex2D(_MainTex,i.uv).rgb;
				  float d = abs(length(abs(_keyingColor.rgb - input_color.rgb)));
				  float edge0 = _thresh * (1 - _slope);
				  float alpha = smoothstep(edge0,_thresh,d);
				  return float4(input_color,alpha);


				  }

				ENDCG
			}
		}

			FallBack "Unlit/Texture"
}

/*
	Properties{
			_MainTex("Base (RGB)", 2D) = "white" {}
			_MaskCol("Mask Color", Color) = (1.0, 0.0, 0.0, 1.0)
			_Sensitivity("Threshold Sensitivity", Range(0,1)) = 0.5
			_Smooth("Smoothing", Range(0,1)) = 0.5
	}
		SubShader{
				Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
				LOD 100
				ZTest Always Cull Back ZWrite On Lighting Off Fog { Mode off }
				CGPROGRAM
				#pragma surface surf Lambert alpha

				struct Input {
					float2 uv_MainTex;
				};

				sampler2D _MainTex;
				float4 _MaskCol;
				float _Sensitivity;
				float _Smooth;

				void surf(Input IN, inout SurfaceOutput o) {
						half4 c = tex2D(_MainTex, IN.uv_MainTex);

						float maskY = 0.2989 * _MaskCol.r + 0.5866 * _MaskCol.g + 0.1145 * _MaskCol.b;
						float maskCr = 0.7132 * (_MaskCol.r - maskY);
						float maskCb = 0.5647 * (_MaskCol.b - maskY);

						float Y = 0.2989 * c.r + 0.5866 * c.g + 0.1145 * c.b;
						float Cr = 0.7132 * (c.r - Y);
						float Cb = 0.5647 * (c.b - Y);

						float blendValue = 1.0f - smoothstep(_Sensitivity, _Sensitivity + _Smooth, distance(float2(Cr, Cb), float2(maskCr, maskCb)));
						o.Alpha = 1.0 * blendValue;
						o.Emission = c.rgb * blendValue;
				}
				ENDCG
			}
				FallBack "Diffuse"
}*/