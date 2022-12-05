// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR_Experience/sketchShader" {
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_LS("Contrast", Range(1,15)) = 8
		_Whiteness("Whiteness", Range(0,2)) = 0.1
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
			fixed _LS;
			fixed _Whiteness;

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
				float LS = _LS;
				float width = 1150;
				float height = 750;
				//float width = 612.0;
				//float height = 460.0;
				fixed3 W = fixed3(0.2125, 0.7154, 0.0721);
				fixed4 textureColor = tex2D(_MainTex, vIn.uv)*LS;

				fixed2 stp0 = fixed2(1.0 / width, 0.0);
				fixed2 st0p = fixed2(0.0, 1.0 / width);
				fixed2 stpp = fixed2(1.0 / width, 1.0 / height);
				fixed2 stpm = fixed2(1.0 / width, -1.0 / height);

				float i00 = dot(textureColor, W);
				float im1m1 = dot(tex2D(_MainTex, vIn.uv - stpp).rgb*LS, W);
				float ip1p1 = dot(tex2D(_MainTex, vIn.uv + stpp).rgb*LS, W);
				float im1p1 = dot(tex2D(_MainTex, vIn.uv - stpm).rgb*LS, W);
				float ip1m1 = dot(tex2D(_MainTex, vIn.uv + stpm).rgb*LS, W);
				float im10 = dot(tex2D(_MainTex, vIn.uv - stp0).rgb*LS, W);
				float ip10 = dot(tex2D(_MainTex, vIn.uv + stp0).rgb*LS, W);
				float i0m1 = dot(tex2D(_MainTex, vIn.uv - st0p).rgb*LS, W);
				float i0p1 = dot(tex2D(_MainTex, vIn.uv + st0p).rgb*LS, W);
				float h = -im1p1 - 2.0 * i0p1 - ip1p1 + im1m1 + 2.0 * i0m1 + ip1m1;
				float v = -im1m1 - 2.0 * im10 - im1p1 + ip1m1 + 2.0 * ip10 + ip1p1;

				float mag = _Whiteness - length(fixed2(h, v));
				fixed4 target = fixed4(mag, mag, mag, 1);

				return lerp(target, textureColor, 0.5);
		}
		ENDCG
		}
	}

	FallBack "Diffuse"
}
