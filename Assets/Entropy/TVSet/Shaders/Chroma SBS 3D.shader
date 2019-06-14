Shader "Custom/Chroma SBS 3D" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_thresh("Threshold", Range(0, 16)) = 0.8
		_slope("Slope", Range(0, 1)) = 0.2
		_keyingColor("Key Colour", Color) = (1,1,1,1)
	}
	SubShader {
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		LOD 200

		Lighting Off
		AlphaTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Unlit alphatest:_Cutoff

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		float3 _keyingColor;
		float _thresh; // 0.8
		float _slope; // 0.2
		
		struct Input {
			float2 uv_MainTex;
		};


		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten) {
			half4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}

		void surf(Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			float2 uv = IN.uv_MainTex* float2(0.5, 1) + float2(0.5*(unity_StereoEyeIndex), 0);
			
			float3 input_color = tex2D(_MainTex, uv).rgb;
			
			float d = abs(length(abs(_keyingColor.rgb - input_color.rgb)));
			float edge0 = _thresh * (1 - _slope);
			float alpha = smoothstep(edge0,_thresh,d);
			
			
			o.Albedo = input_color;
			o.Alpha = alpha;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
