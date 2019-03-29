Shader "RecoShader" { // defines the name of the shader 
	SubShader { // Unity chooses the subshader that fits the GPU best
      Pass { // some shaders require multiple passes
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 100
        Cull Front
		CGPROGRAM // here begins the part in Unity's Cg
			#pragma vertex		VS_Main
			#pragma fragment	FS_Main
/// DATA STRUCTURES ====================================
  			struct VS_Input {
				float4 vertex 	: POSITION;
				float3 normal	: NORMAL;
				float3 color	: COLOR;
			 };
                 
			 struct FS_Input {
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				float2 tex1 : TEXCOORD0;
			 };
/// ==================================================
 			uniform float4x4	Intrinsics;
			uniform float4x4	Global2LocalColor;
 			uniform sampler2D	Texture;
			uniform float4		Texture_TexelSize;
/// ================================================== 	    
 
 
/// VERTEX SHADER ====================================
			float2 computeUV(int textureID, float4 pos) {
				/// WARNING: This assumes that the position attribute of each vertex is measured in meters.
				/// Here convertion to millimeters is taking place in order to be compatible with Kinect intrinsics & extrinsics
				pos = float4(pos.x, pos.y, -pos.z, 0.001f) *1000.0;
				float4 ppos = mul(Global2LocalColor, pos);
				float4 cpos = float4((ppos / ppos.z).xyz, 1.0f);
				float2 uv   = mul(Intrinsics, cpos).xy;
				return uv / float2(Texture_TexelSize.z, Texture_TexelSize.w);
			}

			FS_Input VS_Main(VS_Input input) {
			FS_Input output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.normal = input.normal;
				output.tex1 = computeUV(0, input.vertex);	    
			return output;
			}
/// ========================================================================
 
 
/// FRAGMENT SHADER ========================================================
			float4 FS_Main(FS_Input input) : COLOR // fragment shader
			{         	
				return tex2D(Texture, input.tex1) * 2;
			}
/// ==========================================================================      

         ENDCG // here ends the part in Cg 
      }
   }
}