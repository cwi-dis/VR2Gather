// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR_Experience/thermalShader" {
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Shade("Shade", Range(1,10)) = 2.5
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
			fixed _Shade;

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
				float3 tc = float3(1.0, 0.0, 0.0);
				fixed4 col = tex2D(_MainTex, vIn.uv);
				float3 colors[3];
				colors[0] = float3(0.0, 0.0, 1.0);
				colors[1] = float3(1.0, 1.0, 0.0);
				colors[2] = float3(1.0, 0.0, 0.0);
				float lum = (col.r + col.g + col.b) / _Shade;
				int ix = (lum < 0.5) ? 0 : 1;
				tc = lerp(colors[ix], colors[ix + 1], (lum - float(ix)*0.5) / 0.5);

				return fixed4(tc, 1.0);
		}
		ENDCG
		}
	}

	FallBack "Diffuse"
}