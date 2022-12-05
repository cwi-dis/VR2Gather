Shader "ViveSR_Experience/touchpadUnlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_StripColor("StripColor", Color) = (1,1,1,1)
		_Animation("Animation", Range(0, 1)) = 1
		_StripMin("StripMin", Range(0, 1)) = 0.22
		_StripMax("StripMax", Range(0, 1)) = 0.48

		[Enum(Vive.Plugin.SR.Experience.TouchpadDirection)] _TouchpadDirection("TouchpadDirection", Int) = 0
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Transparent"
			"Queue" = "Transparent"
			"CanUseSpriteAtlas" = "True"
		}



		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100
		Cull Front

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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _StripColor;
			float _Animation;
			float _StripMin;
			float _StripMax;
			int _TouchpadDirection;
			float4 _colorArray[5];

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
			}

			float map(float value, float min1, float max1, float min2, float max2)
			{
				return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
			}

			float touchpadDirectionOf(float2 axis)
			{
				float deg;

				float2 imageCenter = float2(0.5, 0.5);

				float touchpadDirection = 0;

				if (distance(axis, imageCenter) < _StripMin) return 5;//mid
				axis = float2(axis.x - 0.5, axis.y - 0.5);

				if (axis.x == 0) deg = axis.y >= 0 ? 90 : -90;
				else deg = atan(axis.y / axis.x) * 57.2958;

				if (axis.x >= 0)
				{
					if (deg >= 45) touchpadDirection = 1; //up;
					else if (deg < 45 && deg > -45) touchpadDirection = 3; //right;
					else if (deg <= -45) touchpadDirection = 2; //down;
				}
				else
				{
					if (deg >= 45) touchpadDirection = 2; //down;
					else if (deg < 45 && deg > -45) touchpadDirection = 4; //left;
					else if (deg <= -45) touchpadDirection = 1; //up;
				}

				return touchpadDirection;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				fixed4 tex_c = tex2D(_MainTex, uv);

				float touchpadDirection = touchpadDirectionOf(uv);

				fixed4 col = tex_c * _colorArray[touchpadDirection-1];
				float2 imageCenter = float2(0.5, 0.5);

				float dist = distance(imageCenter, uv);
				if (dist > 0.4 && col.a < 0.5) { clip(-1); }

				if (touchpadDirection == _TouchpadDirection)
				{
					if (_TouchpadDirection == 5)
					{							
						float _Glow = _Animation;

						if (dist < _StripMin)
						{
							dist = map(dist, 0, _StripMin, 0, 0.5);
							col.rgb = lerp(col.rgb, _StripColor, pow(5, 4 * _Glow * dist) * 0.015);
						}
					}
					else
					{
						float _StripPos = _Animation;

						float stripWidth = _StripMax - _StripMin;

						if (dist >= (_StripMin - 0.06) + _StripPos*stripWidth && dist <= (_StripMin - 0.05) + _StripPos*stripWidth)
							col.rgb = lerp(col.rgb, _StripColor, _StripPos * dist * 2);
						if (dist >= _StripMin + _StripPos*stripWidth && dist <= _StripMin + 0.02 + _StripPos*stripWidth)
							col.rgb = lerp(col.rgb, _StripColor, _StripPos * dist * 2);
					}
				}

				return col;
			}
			ENDCG
		}
	}
}
