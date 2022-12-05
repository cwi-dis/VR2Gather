Shader "Music2Dance1980/VolumetricLightShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Thickness("Thickness", Float) = 1
		_Strength("Strength", Range(0,0.5)) = 0.01
		_Edge("Edge",  Range(0, 20)) = 3
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
		LOD 100

		Cull Off
		ZWrite Off
		Blend SrcAlpha One

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
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Thickness, _Strength, _Edge;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;

				half a_min = 0;
				half a_max = 0.5;
				half b_min = 0;
				half b_max = 1;
				half dist = (distance(half2(0.5, 0.5), i.uv.y) - a_min) / (a_max - a_min) * (b_max - b_min) + b_min;

				fixed fade = saturate(_Strength * pow(dist, -_Edge));

				col.a = col.a * lerp(0, 1, fade * _Thickness);
																  
				return col;
			}
			ENDCG
		}
	}
}
