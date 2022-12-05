Shader "Music2Dance1980/DiscoFloorPieceShader" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_GridTex("GridTex (RGB)", 2D) = "white" {}
		_GridColor("GridColor", Color) = (1,1,1,1)

		_GradiantTex("GradiantTex (RGB)", 2D) = "white" {}
		_GradiantColor("GradiantColor", Color) = (1,1,1,1)


		_ColorCount("ColorCount", Int) = 5
		_Emission("Emission", Float) = 1

	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue" = "Geometry" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
			
		sampler2D _MainTex;
		sampler2D _GridTex, _GradiantTex;

		struct appdata {
			half4 vertex: POSITION;
			half3 normal: NORMAL;
			half2 texcoord: TEXCOORD0;
			half2 texcoord1: TEXCOORD1;
			half2 texcoord2: TEXCOORD2;
			half4 tangent: TANGENT;
		};

		struct Input {
			float2 uv_MainTex;
			fixed3 vert;
		};

		int _ColorCount;
		fixed4 _Colors[5];
		float _IsEmissionOn[5];
		fixed4 _GridColor;

		float _Emission;

		void vert(inout appdata v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vert.xyz = v.vertex.xyz;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {

			for (int i = 0; i < _ColorCount; ++i) {

				if(_Colors[i].a == 0)
					_Colors[i] = fixed4(1,1,1,1);
			}

			fixed2 uv = IN.uv_MainTex;
			fixed4 c = tex2D(_MainTex, uv);
			fixed4 grid = tex2D(_GridTex, uv * fixed2(8, 8));
			fixed4 gradiant = tex2D(_GradiantTex, uv * fixed2(8, 8));


			//for (int i = 0; i < ColorCount; ++i);
		
			if (grid.a > 0.1) c = _GridColor;
			else {
				int index;
				if (c.a == 1) index = 0;
				else if (c.a == 0) index = _ColorCount - 1;
				else index = c.a / (1.0 / _ColorCount);
				c = _Colors[index];
				o.Emission = (_IsEmissionOn[index] == 1) ? c.rgb * _Emission * gradiant.a : 0;
			}
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
