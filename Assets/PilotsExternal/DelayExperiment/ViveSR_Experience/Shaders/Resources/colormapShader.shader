Shader "ViveSR_Experience/colormapShader" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_colorMap("colormap", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200

		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		#pragma surface surf NoLighting noambient alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _colorMap;
	struct Input {
		float2 uv_MainTex;
	};


	fixed4 _Color;

	fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
	{	
		return fixed4(s.Albedo, s.Alpha);
	}
	
	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed2 uv = IN.uv_MainTex;


		fixed3 col = tex2D(_MainTex, fixed2(uv.x,1-uv.y)).rgb;

		if (col.x < 0)
		{
			o.Albedo = fixed3(0.0,0.0,0.0);
		}
		else{
			if (col.x > 250)
			{
				col.x = 250;
			}
				
			if (col.x < 30)
			{
				col.x = 30;
			}
		
			fixed2 new_uv = fixed2(0.5, (col.x - 30.0) / 220);
			if (new_uv.y < 0.05)
				new_uv.y = 0.05;
			if (new_uv.y > 0.95)
				new_uv.y = 0.95;


			fixed3 colMap = tex2D(_colorMap, new_uv);

			o.Albedo = colMap;

		}
		o.Alpha = _Color.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}
