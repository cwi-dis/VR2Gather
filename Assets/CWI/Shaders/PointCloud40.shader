Shader "Entropy/PointCloud40"{
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert //vertex:vert

		struct Input {
			float4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			o.Albedo = IN.color.rgb;
			//o.Alpha = 1;
		}
		ENDCG
	}
}