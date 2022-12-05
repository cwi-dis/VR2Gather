// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR_Experience/sharpShader" {
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_SharpBright("Brightness", Range(9,15)) = 9
		_SharpIntense("SharpIntensity", Range(0.2,5)) = 1.8
		//_StencilValue ("StencilRefValue", float) = 1
		//[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Compare", int) = 0	// disable
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Cull Off ZWrite Off ZTest Always

		//Stencil{
		//	Ref  [_StencilValue]
		//	Comp [_StencilComp]
		//}

		Pass
		{
			CGPROGRAM

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _MainTex_TexelSize;
			fixed _SharpBright;
			fixed _SharpIntense;

			struct v2f {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};			

			v2f vert (appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				return o;
			}

			fixed4 frag(v2f vIn) : SV_Target
			{
				fixed2 uv = vIn.uv;
				fixed3 sample_F[9];
				fixed3 vFragColour;
				fixed2 _MainTex_size = fixed2(1150, 750);
				//fixed2 _MainTex_size = fixed2(612.0, 460.0);

				for (int j = 0; j < 3; ++j) {
					for (int i = 0; i < 3; ++i) {
						sample_F[j * 3 + i] = tex2D(_MainTex, uv + fixed2(i - 2, j - 2) / _MainTex_size ).rgb * _SharpIntense;
					}
				}
				vFragColour = _SharpBright * sample_F[4];

				for (int i = 0; i < 9; i++)
				{
					if (i != 4)
						vFragColour -= (sample_F[i]*1.04);
				}

				return fixed4(vFragColour, 1.0);
		}
		ENDCG
		}
	}

	FallBack "Diffuse"
}