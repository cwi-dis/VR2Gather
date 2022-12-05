Shader "ViveSR_Experience/viveDeerShader" {
	Properties {
		_Hue("Hue", Range(0, 1)) = 0
		[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_StencilValue ("StencilRefValue", float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Compare", int) = 0	// disable
		[Enum(UnityEngine.Rendering.CompareFunction)]_ZTestComp("ZTest Compare", int) = 4		// lequal
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		ZTest[_ZTestComp]
		Stencil{
			Ref [_StencilValue]
			Comp[_StencilComp]
		}

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
		#pragma multi_compile __  CLIP_PLANE

		float3 changeHue(float3 rgb, float3 target_hsv)
		{ 
			float3 tempRGB = rgb;
			float u = target_hsv.z * target_hsv.y * cos(target_hsv.x * 0.0174);
			float w = target_hsv.z * target_hsv.y * sin(target_hsv.x * 0.0174);

			tempRGB.x = rgb.x * (0.299 * target_hsv.z + u * 0.701 + w * 0.168) +
				        rgb.y * (0.587 * target_hsv.z - u * 0.587 + w * 0.330) +
			            rgb.z * (0.114 * target_hsv.z - u * 0.114 - w * 0.497);
			tempRGB.y = rgb.x * (0.299 * target_hsv.z - u * 0.299 - w * 0.328) +
					    rgb.y * (0.587 * target_hsv.z + u * 0.413 + w * 0.035) +
				        rgb.z * (0.114 * target_hsv.z - u * 0.114 + w * 0.292);
			tempRGB.z = rgb.x * (0.299 * target_hsv.z - u * 0.300 + w * 1.250) +
					    rgb.y * (0.587 * target_hsv.z - u * 0.588 - w * 1.050) +
				        rgb.z * (0.114 * target_hsv.z + u * 0.886 - w * 0.203);

			return tempRGB;
		}

		struct Input{
			float2 uv_MainTex;
		};

		sampler2D _MainTex;
		float _Glossiness;
		float _Metallic;
		float _Hue;

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			_Hue = 360 * _Hue;
			c.rgb = changeHue(c.rgb, float3(_Hue, 1, 1));
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
