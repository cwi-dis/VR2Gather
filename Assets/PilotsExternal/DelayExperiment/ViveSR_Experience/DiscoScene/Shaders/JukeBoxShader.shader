Shader "Custom/JukeBoxShader" {
	Properties {
		_Speed("Speed", Range(0, 1)) = 0.5
		[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _MetallicTex("Metallic (RGB)", 2D) = "white" {}
		[NoScaleOffset] _BumpMap("Bumpmap", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		[NoScaleOffset] _EmissionTex("Emission (RGB)", 2D) = "white" {}
		[NoScaleOffset] _EmissionMask("EmissionMask", 2D) = "white" {}
		_Emission("Emission", Range(0,1)) = 0.5
	}

	SubShader
	{
		Name "JukeBox"
		//	
		Stencil
		{
			Ref 3
			Comp always
			Pass replace
		}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Cull Back
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _MetallicTex;
		sampler2D _BumpMap;
		sampler2D _EmissionTex, _EmissionMask;

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


		half _Glossiness;
		half _Metallic;
		half _Emission;
		half _Speed;

		struct Input
		{
			fixed2 uv_MainTex;
		};
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 m = tex2D(_MetallicTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			o.Smoothness = m.a * _Glossiness;
			o.Alpha = c.a;
			o.Metallic = m * _Metallic;


			fixed4 e = tex2D(_EmissionTex, IN.uv_MainTex);
			fixed4 e_mask = tex2D(_EmissionMask, IN.uv_MainTex);
			
			e.rgb = changeHue(e.rgb, float3(360 * _Time.y * _Speed, 1, 1)) * e_mask;

			o.Emission = e * _Emission;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
