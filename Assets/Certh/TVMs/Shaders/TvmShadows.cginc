// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(TVM_SHADOWS_INCLUDED)
#define TVM_SHADOWS_INCLUDED

#include "UnityCG.cginc"

struct VertexData {
	float4 position : POSITION;
};

float4 TvmShadowVertexProgram(VertexData v) : SV_POSITION{
	return UnityObjectToClipPos(v.position);
}

half4 TvmShadowFragmentProgram() : SV_TARGET{
	return 0;
}

#endif
