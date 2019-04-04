Shader "Entropy/PointCloud40"{
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert //vertex:vert
		/*
		void vert(inout appdata_full v)
		{
		
		}*/

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			o.Albedo = IN.color.rgb;
			o.Alpha = 1;
		}
		ENDCG
	}
}