Shader "Entropy/MonoSprite" {
	Properties{
			_MainTex("Texture", 2D) = "white" {}
			_Color("Color Tint", Color) = (1,1,1,1)
	}

		SubShader {
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 200
			
			Pass {
				Blend srcalpha oneminussrcalpha
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				sampler2D	_MainTex;
				float4		_Color;

				v2f vert(appdata v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv * float2(1,0.5);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					fixed4 col = tex2D(_MainTex, i.uv + fixed2(0, 0.5) );
					fixed guide = tex2D(_MainTex, i.uv + fixed2(0, 0) );
					if (guide < 0.063f) // 16/255 as black
						discard;
					else
						col.a = guide;
					return col;
				}
			ENDCG
		}
	}
}
