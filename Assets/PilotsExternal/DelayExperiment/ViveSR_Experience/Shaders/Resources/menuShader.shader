Shader "ViveSR_Experience/menuShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_FrameTex("Albedo (RGB)", 2D) = "white" {}
		[Toggle] _Hovered("Hovered", Int) = 0
	}
	SubShader {
		Tags { "Queue" = "Transparent-1" "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf NoLighting noambient alpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _FrameTex;

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			return fixed4(s.Albedo, s.Alpha);
		}

		struct Input
		{
			float2 uv_MainTex;	
			float2 uv_FrameTex;
		};

		fixed4 _Color;
		int _Hovered;

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			if(_Hovered && c.a < 0.2) c = tex2D(_FrameTex, IN.uv_FrameTex);

			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
