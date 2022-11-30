Shader "Custom/ChromaKeyRemove" {

	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_MainTex2("Base (RGB)", 2D) = "white" {}
		_AlphaValue("Alpha Value", Range(0.0,0.2)) = 0.0
		//_Distance_TH("Distance threshold",Range(0.0,1.0)) = 1.0


		_AlphaValue2("Alpha Value2", Range(0.0,0.2)) = 0.0
		_Distance_TH2("Distance threshold2",Range(0.0,1.0)) = 1.0
		MarginXLow("MarginXLow", Range(0.0,1.0)) = 0.0
		MarginXHigh("MarginXHigh", Range(0.0,1.0)) = 0.0
		MarginYLow("MarginYLow", Range(0.0,1.0)) = 0.0
		MarginYHigh("MarginYHigh", Range(0.0,1.0)) = 0.0


	}

		SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
			Lighting Off
			Cull Off
			ZTest Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask[_ColorMask]
		CGPROGRAM
#pragma surface surf Lambert alpha

	sampler2D _MainTex;
	sampler2D _MainTex2;
	float _AlphaValue;
	float _AlphaValue2;

	float MarginXLow;
	float MarginXHigh;
	float MarginYLow;
	float MarginYHigh;

	float3 u_crth;
	float _Distance_TH;

	float _Distance_TH2;
	float _spatialXTh;
	float scaleX;
	float scaleY;
	float cr;


	struct Input {
		float2 uv_MainTex;
		float2 uv_MainTex2;
		float4 screenPos;
	};

	void surf(Input IN, inout SurfaceOutput o) {

		o.Alpha = 0;
		IN.uv_MainTex2.y = 1 - IN.uv_MainTex2.y;
		half4 other = tex2D(_MainTex2, IN.uv_MainTex2);

		
		//if (color2.g < 0.01f) return;
		half4 color = tex2D(_MainTex, IN.uv_MainTex);
		float finalpha;
		float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
		
		o.Emission = color.rgb;
		if (screenUV.x > MarginXLow && screenUV.x < MarginXHigh && screenUV.y > MarginYLow && screenUV.y < MarginYHigh) {
			cr = 0.5 * color.r + -0.418688 * color.g + -0.081312 * color.b;
			if (other.a > 0.01) {
				cr = -0.5 * color.r + 0.5 * color.g + 0 * color.b;
				float alpha = _AlphaValue2 - cr;
				float denom = _AlphaValue2;
				float ag = clamp(alpha / denom, 0.0, 1.0);
				finalpha = max(ag, _Distance_TH2);//_Distance_TH); removing chroma
				//float finalpha = max(ag, _Distance_TH);//_Distance_TH);
			}
			else {
				float alpha = cr - _AlphaValue;
				float denom = _AlphaValue;
				float ag = clamp(alpha / denom, 0.0, 1.0);
				finalpha = max(ag, _Distance_TH);//_Distance_TH); removing chroma
				//float finalpha = max(ag, _Distance_TH);//_Distance_TH);
			}
		}
		else {
			finalpha = 0;
		}

		o.Alpha = finalpha;


		//if (other.r < 0.01) { o.Alpha = 0; }
		/*if (other.r < 0.01) {
			o.Alpha = 0;
		}*/
		
		
		//o = vec4(color.r, color.g, color.b, finalpha);
		//half4 c = tex2D(_MainTex, IN.uv_MainTex);
		
		/*float Dis = distance(c, _color_TH);

		if (Dis > _Distance_TH)
			//c.g >= _Green_TH && c.r <= _Red_TH && c.b <= _Blue_TH && _AlphaValue == 0.0)
		{
			o.Alpha = 0.0f;
		}
		else
		{
			o.Alpha = c.a;
		}*/

	}
	ENDCG
	}
		FallBack "Diffuse"
}