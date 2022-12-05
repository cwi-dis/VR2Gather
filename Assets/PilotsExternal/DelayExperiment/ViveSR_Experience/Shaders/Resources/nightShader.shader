// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR_Experience/nightShader" {
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_VisionBright("Brightness", Range(0,5)) = 1.3
		_Radius("Radius", Range(0,3)) = 0.4
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
			fixed _VisionBright;
			fixed _Radius;

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

				fixed3 c = tex2D(_MainTex, uv).rgb;
				float d = length(fixed2(uv.x - 0.5, uv.y - 0.56)) * _Radius;

				fixed4 cycle = fixed4(1.2 - d / 0.2, 1.0 - d / 0.2, 1.0 - d / 0.2, 1.0) * _VisionBright;

				return fixed4(c.x*cycle.x*0.1, c.y*cycle.x, c.z*cycle.x*0.2, 1.0);
				//return (d > _Radius) ? fixed4(0.0, 0.0, 0.0, 1.0) : fixed4(c.x*cycle.x*0.1, c.y*cycle.x, c.z*cycle.x*0.2, 1.0);
		}
		ENDCG
		}
	}

	FallBack "Diffuse"
}
