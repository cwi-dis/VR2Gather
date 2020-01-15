Shader "Hidden/ftSubtract"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                #if UNITY_UV_STARTS_AT_TOP
                o.uv = v.uv;
                #else
                o.uv = v.uv;
                o.uv.y = 1-o.uv.y;
                #endif
				return o;
			}

			sampler2D texA, texB;

			fixed4 frag (v2f i) : SV_Target
			{
                float4 a = tex2D(texA, i.uv);
                float4 b = tex2D(texB, i.uv);
                float3 c = a.rgb - b.rgb;
				return float4(c, a.a);
			}
			ENDCG
		}
	}
}
