Shader "RecoShader" { // defines the name of the shader 
	Properties{
		Texture0("Texture0", 2D) = "black"
		Texture1("Texture1", 2D) = "black"
		Texture2("Texture2", 2D) = "black"
		Texture3("Texture3", 2D) = "black"
	}
	SubShader{ // Unity chooses the subshader that fits the GPU best
	  Pass { // some shaders require multiple passes
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 100
		Cull Back
		CGPROGRAM // here begins the part in Unity's Cg
			#pragma vertex		VS_Main
			#pragma fragment	FS_Main
			#pragma geometry    GS_Main
/// DATA STRUCTURES ====================================
			struct VS_Input {
				float4 vertex 	: POSITION;
				float3 normal	: NORMAL;
				float3 color	: COLOR;
			 };
			struct GS_Input {
				float4 vertex 	: POSITION;
				float3 normal	: NORMAL;
				float3 ids		: TEXCOORD0;
			};

			 struct FS_Input {
				float4 pos    : SV_POSITION;
				float3 normal : NORMAL;
				float2 tex1   : TEXCOORD0;
				float2 tex2   : TEXCOORD1;
				int2   ids    : TEXCOORD2;
				float  lerp   : TEXCOORD3;
			 };
			 /// ==================================================
			 uniform sampler2D	Texture0;
			 uniform float4		Texture0_TexelSize;
			 uniform float4x4	Intrinsics0;
			 uniform float4x4	Global2Local0;

			 uniform sampler2D	Texture1;
			 uniform float4		Texture1_TexelSize;
			 uniform float4x4	Intrinsics1;
			 uniform float4x4	Global2Local1;

			 uniform sampler2D	Texture2;
			 uniform float4		Texture2_TexelSize;
			 uniform float4x4	Intrinsics2;
			 uniform float4x4	Global2Local2;

			 uniform sampler2D	Texture3;
			 uniform float4		Texture3_TexelSize;
			 uniform float4x4	Intrinsics3;
			 uniform float4x4	Global2Local3;
			 /// ================================================== 	    


			 /// VERTEX SHADER ====================================
			 float2 computeUV(int textureID, float4 pos) {
				 pos = float4(pos.x, pos.y, -pos.z, 1.0f);
				 float4 ppos;
				 float4 cpos;
				 float2 uv;
				 if (textureID < 2) {
					 if (textureID == 0) {
						 ppos = mul(Global2Local0, pos);
						 cpos = float4((ppos / ppos.z).xyz, 1.0f);
						 uv = mul(Intrinsics0, cpos).xy;
					 }
					 else {
						 ppos = mul(Global2Local1, pos);
						 cpos = float4((ppos / ppos.z).xyz, 1.0f);
						 uv = mul(Intrinsics1, cpos).xy;
					 }
				 }
				 else {
					 if (textureID == 2) {
						 ppos = mul(Global2Local2, pos);
						 cpos = float4((ppos / ppos.z).xyz, 1.0f);
						 uv = mul(Intrinsics2, cpos).xy;
					 }
					 else{
						 ppos = mul(Global2Local3, pos);
						 cpos = float4((ppos / ppos.z).xyz, 1.0f);
						 uv = mul(Intrinsics3, cpos).xy;
					 }
				 }
				 return uv / float2(Texture0_TexelSize.z, Texture0_TexelSize.w);
			 }

			 GS_Input VS_Main(VS_Input input)
			 {
				 GS_Input output;
				 output.vertex = input.vertex;
				 output.normal = input.normal;
				 output.ids    = input.color;
				 return output;

			 }
			 /*
			 FS_Input VS_Main(VS_Input input) {
				 FS_Input output;
				 output.pos = UnityObjectToClipPos(input.vertex);
				 output.normal = input.normal;

				 output.ids = floor(input.color.xy);
				 output.lerp = input.color.z;
				 output.tex1 = computeUV(output.ids.x, input.vertex);
				 output.tex2 = computeUV(output.ids.y, input.vertex);

				 return output;
			 }
			 */

			 [maxvertexcount(3)]
			 void GS_Main(triangle GS_Input t[3], inout TriangleStream<FS_Input> triStream)
			 {
				 bool ok = false;
				 if ((t[0].ids.x >= 0) && (t[0].ids.y >= 0)) {
					 t[1].ids = t[0].ids;
					 t[2].ids = t[0].ids;
					 ok = true;
				 }
				 else if ((t[1].ids.x >= 0) && (t[1].ids.y >= 0)) {
					 t[0].ids =  t[1].ids;
					 t[2].ids = t[1].ids;
					 ok = true;
				 }
				 else if ((t[2].ids.x >= 0) && (t[2].ids.y >= 0)) {
					 t[0].ids = t[2].ids;
					 t[1].ids = t[2].ids;
					 ok = true;
				 }
				 if (ok) {
					 FS_Input outt[3];
					 for (int i = 2; i >= 0; i--) {			// for backface culling reverse order of triangles, because we negated z
					 //for(int i=0;i<=2;i++) {		
						 float4 v = t[i].vertex;
						 outt[i].pos = UnityObjectToClipPos(v);
						 outt[i].normal = t[i].normal;
						 outt[i].tex1	= computeUV(floor(t[i].ids.x), v);
						 outt[i].tex2	= computeUV(floor(t[i].ids.y), v);
						 outt[i].ids	= floor(t[i].ids.xy);
						 outt[i].lerp   = t[i].ids.z;

						 triStream.Append(outt[i]);
					 }

				 }
			 }

			 /// FRAGMENT SHADER ========================================================
			 float4 sampleTexture(int texID, float2 uv) {
				 if (texID < 2) {
					 if (texID == 0)		return tex2D(Texture0, uv);
					 else					return tex2D(Texture1, uv);
				 }
				 else {
					 if (texID == 2)		return tex2D(Texture2, uv);
					 else					return tex2D(Texture3, uv);
				 }
			 }


			 float4 FS_Main(FS_Input input) : COLOR // fragment shader
			 {
				 int texID1 = input.ids.x;
				 int texID2 = input.ids.y;

				 float4 color1 = sampleTexture(texID1, input.tex1);
				 float4 color2 = sampleTexture(texID2, input.tex2);
				 return lerp(color1, color2, 1 - input.lerp);
			 }
				 /// ==========================================================================      

			ENDCG // here ends the part in Cg 
		}	
	}
}