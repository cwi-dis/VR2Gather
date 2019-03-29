Shader "Unlit/Screen"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_StencilVal("stencilVal", Int) = 1
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry+1"}
		LOD 100
		Pass
		{
			Stencil {
				Ref [_StencilVal]
				Comp always
				Pass replace
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(1,0,0,1);
			}
			ENDCG
		}
	}
}
