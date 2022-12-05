// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Skybox/6 Sided_edited" {
Properties {
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0

	_EffectTileX("EffectTileX", float) = 1
	_EffectTileY("EffectTileY", float) = 1
	_EffectRotation("EffectRotation", float) = 0
	_EffectStrength("EffectStrength", Range(0,0.5)) = 0.01
	_EffectEdge("EffectEdge",  Range(0, 20)) = 3
	[Gamma] _EffectExposure("EffectExposure", Range(0, 1)) = 1.0
	[NoScaleOffset] _FrontTex ("Front [+Z]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _BackTex ("Back [-Z]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _LeftTex ("Left [+X]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _RightTex ("Right [-X]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _UpTex ("Up [+Y]   (HDR)", 2D) = "grey" {}
    [NoScaleOffset] _DownTex ("Down [-Y]   (HDR)", 2D) = "grey" {}

	[NoScaleOffset] _EffectTex("EffectTex", 2D) = "grey" {}
	_EffectColor("EffectColor", Color) = (0, 0, 0, 0)
	_EffectColor1("EffectColor1", Color) = (0, 0, 0, 0)
	_EffectColor2("EffectColor2", Color) = (0, 0, 0, 0)
	_EffectColor3("EffectColor3", Color) = (0, 0, 0, 0)
	_EffectColor4("EffectColor4", Color) = (0, 0, 0, 0)
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    CGINCLUDE
    #include "UnityCG.cginc"

    half _Exposure, _EffectExposure;
    float _Rotation, _EffectRotation , _EffectStrength, _EffectEdge, _EffectTileX, _EffectTileY;
	sampler2D _EffectTex;
	half4 _EffectColor, _EffectColor1, _EffectColor2, _EffectColor3, _EffectColor4;

    float3 RotateAroundYInDegrees (float3 vertex, float degrees)
    {
        float alpha = degrees * UNITY_PI / 180.0;
        float sina, cosa;
        sincos(alpha, sina, cosa);
        float2x2 m = float2x2(cosa, -sina, sina, cosa);
        return float3(mul(m, vertex.xz), vertex.y).xzy;
    }

    struct appdata_t {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    struct v2f {
        float4 vertex : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    v2f vert (appdata_t v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
        o.vertex = UnityObjectToClipPos(rotated);
        o.texcoord = v.texcoord;
        return o;
    }


	half map(half value_in, half min, half max)
	{
		half a_min = 0;
		half a_max = 0.2;
		half b_min = 0;
		half b_max = 1;
		half value = (value_in - a_min) / (a_max - a_min) * (b_max - b_min) + b_min;
		return value;
	}

    half4 skybox_frag_withDots (v2f i, sampler2D smp, half4 smpDecode)
    {
        half4 tex = tex2D (smp, i.texcoord);
		half4 effectTex = tex2D(_EffectTex, i.texcoord * fixed2(_EffectTileX, _EffectTileY) + fixed2(_EffectRotation, 0));

		half3 c = DecodeHDR(tex, smpDecode);
		half3 c_effect = DecodeHDR(effectTex, smpDecode);

		fixed effect_a = (c_effect.r + c_effect.g + c_effect.b) / 3;

		c_effect = lerp(half3(0, 0, 0), _EffectColor, map(c_effect, 0.8, 1));
		if (effect_a < 0.2) c_effect = lerp(half3(0,0,0), _EffectColor1, map(c_effect, 0, 0.2));
		else if (effect_a < 0.4) c_effect = lerp(half3(0, 0, 0), _EffectColor2, map(c_effect, 0.2, 0.4));
		else if (effect_a < 0.6) c_effect = lerp(half3(0, 0, 0), _EffectColor3, map(c_effect, 0.4, 0.6));
		else if (effect_a < 0.8) c_effect = lerp(half3(0, 0, 0), _EffectColor4, map(c_effect, 0.6, 0.8));

		half a_min = 0;
		half a_max = 0.5;
		half b_min = 0;
		half b_max = 1;
		half dist = (distance(half2(0.5, 0.5), i.texcoord.y) - a_min) / (a_max - a_min) * (b_max - b_min) + b_min;

		fixed fade = saturate(_EffectStrength * pow(dist, -_EffectEdge));

		c_effect = lerp(fixed4(0, 0, 0, 0), c_effect, fade);

        c = saturate(c*_Exposure + c_effect.rgb * _EffectExposure) * unity_ColorSpaceDouble.rgb;

        return half4(c, 1);
    }

	half4 skybox_frag(v2f i, sampler2D smp, half4 smpDecode)
	{
		half4 tex = tex2D(smp, i.texcoord);

		half3 c = DecodeHDR(tex, smpDecode);
		c = saturate(c *_Exposure) * unity_ColorSpaceDouble.rgb;

		return half4(c, 1);
	}	 

    ENDCG




    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _FrontTex;
        half4 _FrontTex_HDR;
        half4 frag (v2f i) : SV_Target { return skybox_frag_withDots(i,_FrontTex, _FrontTex_HDR); }
        ENDCG
    }
    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _BackTex;
        half4 _BackTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag_withDots(i,_BackTex, _BackTex_HDR); }
        ENDCG
    }
    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _LeftTex;
        half4 _LeftTex_HDR;
        half4 frag (v2f i) : SV_Target { return skybox_frag_withDots(i,_LeftTex, _LeftTex_HDR); }
        ENDCG
    }
    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _RightTex;
        half4 _RightTex_HDR;
        half4 frag (v2f i) : SV_Target { return skybox_frag_withDots(i,_RightTex, _RightTex_HDR); }
        ENDCG
    }
    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _UpTex;
        half4 _UpTex_HDR;
        half4 frag (v2f i) : SV_Target { return skybox_frag(i,_UpTex, _UpTex_HDR); }
        ENDCG
    }
    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        sampler2D _DownTex;
        half4 _DownTex_HDR;
        half4 frag (v2f i) : SV_Target { return skybox_frag(i,_DownTex, _DownTex_HDR); }
        ENDCG
    }
}
}