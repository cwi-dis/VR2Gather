Shader "Music2Dance1980/DiscoBallShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normal", 2D) = "bump" {}
		_Bump("Bump", INT) = 1
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque"  "Queue" = "Geometry" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _NormalMap;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		half _Bump;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			fixed2 uv = IN.uv_MainTex * fixed2(30, 18);
			fixed4 c = 1 - tex2D(_MainTex, uv);
			
			if (c.a > 0.5) c.a = 1;
			else c.a = 0.1;

			o.Normal = 1-(UnpackNormal(tex2D(_NormalMap, uv)) * _Bump);
			o.Albedo = c.a *_Color;
			o.Metallic = c.a * _Metallic;
			o.Smoothness = c.a * _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
