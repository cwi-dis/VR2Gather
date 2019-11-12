Shader "Entropy/360Mono" {
	Properties{
			_MainTex("Texture", 2D) = "white" {}
	}

	SubShader {
		Tags { "Queue" = "Geometry" "RenderType" = "Solid" }
			LOD 200
			
			Pass {
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
					o.uv = v.uv * float2(1, 0.5);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					return  tex2D(_MainTex, i.uv  );
				}
			ENDCG
		}
	}
}
