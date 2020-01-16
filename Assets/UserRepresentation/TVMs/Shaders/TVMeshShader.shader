Shader "Unlit/TVMeshShader"
{
	Properties
	{
	}

	SubShader
	{
		Pass
	{
		CGPROGRAM

		#pragma vertex VS_Main
		#pragma fragment FS_Main
		#pragma geometry GS_Main
		
		#pragma target 3.5
		#define MAX_NUMBER_OF_CAMERAS 8
		#define M_PI 3.141592
		#include "UnityCG.cginc"

		struct VS_Input {
			float4 vertex: POSITION;
			float3 normal: NORMAL;
			float4 ids: TANGENT;
		};

		struct GS_Input {
			float4 vertex 	: POSITION;
			float3 normal	: NORMAL;
			float4 ids  : TANGENT;
			float4 realpos: TEXCOORD0;
		};
		
		struct FS_Input {
			float4 position: SV_POSITION;
			float3 normal: NORMAL;
			int2 ids: TEXCOORD1;
			float2 weights: TEXCOORD2;
			float3 vert: TEXCOORD3;
		};

		uniform int ShadingMode = 1;
		uniform int ShowUntextured = 0;
		uniform float WeightCutoff = 0.05f;

		uniform int CameraNumber;
		uniform int TextureWidth;
		uniform int TextureHeight;
		uniform float ColorIntrinsics[MAX_NUMBER_OF_CAMERAS * 9];
		uniform float ColorExtrinsics[MAX_NUMBER_OF_CAMERAS * 16];
		uniform sampler2D Texture0;
		uniform sampler2D Texture1;
		uniform sampler2D Texture2;
		uniform sampler2D Texture3;
		uniform sampler2D Texture4;
		uniform sampler2D Texture5;
		uniform sampler2D Texture6;
		uniform sampler2D Texture7;

		float4x4 ArrayToMat4(float arr[128], int camId)
		{
			float4x4 result;
			for (int i = 0; i < 4; ++i)
				for (int j = 0; j < 4; ++j)
					result[i][j] = arr[i + j * 4 + 16 * camId]; // column major as a result of Eigen used in the C++ DLL
			return result;
		}

		float3x3 ArrayToMat3(float arr[72], int camId)
		{
			float3x3 result;
			for (int i = 0; i < 3; ++i)
				for (int j = 0; j < 3; ++j)
					result[i][j] = arr[i + j * 3 + 9 * camId]; // column major as a result of Eigen used in the C++ DLL
			return result;
		}

		float2 computeUV(float4x4 invCamExtrinsics, float3x3 camIntrinsics, float3 pos)
		{
			float3 proj = mul(camIntrinsics, (mul(invCamExtrinsics, float4(pos, 1.0f)).xyz));
			float2 projxy = proj.xy / proj.z;
			return projxy / float2(float(TextureWidth), float(TextureHeight));
		}

		float4 findTex(int camID, float2 texUV)
		{
			if (camID == 0)
				return tex2D(Texture0, texUV);
			else if (camID == 1)
				return tex2D(Texture1, texUV);
			else if (camID == 2)
				return tex2D(Texture2, texUV);
			else if (camID == 3)
				return tex2D(Texture3, texUV);
			else if (camID == 4)
				return tex2D(Texture4, texUV);
			else if (camID == 5)
				return tex2D(Texture5, texUV);
			else if (camID == 6)
				return tex2D(Texture6, texUV);
			else if (camID == 7)
				return tex2D(Texture7, texUV);
			else
				return float4(0.0f, 0.0f, 0.0f, 0.0f);
		}

		GS_Input VS_Main(VS_Input IN)
		{
			GS_Input OUT;
			OUT.vertex = float4(IN.vertex.x, IN.vertex.y, -IN.vertex.z, IN.vertex.w);
			OUT.normal = IN.normal;
			OUT.realpos = IN.vertex; 
			OUT.ids = IN.ids;
			return OUT;
		}

		[maxvertexcount(3)]
		void GS_Main(triangle GS_Input t[3], inout TriangleStream<FS_Input> triStream)
		{
			FS_Input outt[3];

			for (int i = 2; i >= 0; i--)
			{			
				// for backface culling reverse order of triangles, because we negated z
				outt[i].position = UnityObjectToClipPos(t[i].vertex);
				outt[i].normal = t[i].normal;
				outt[i].vert = t[i].realpos.xyz;
				outt[i].ids[0] = t[i].ids[0];
				outt[i].ids[1] = t[i].ids[1];
				outt[i].weights[0] = t[i].ids[2];
				outt[i].weights[1] = t[i].ids[3];
				triStream.Append(outt[i]);
			}
		}

		fixed4 FS_Main(FS_Input IN) : SV_Target
		{
			if (IN.ids[0] < 0)
			{
				IN.ids[0] = -1;
			}
			else
			{
				IN.ids[0] = round(IN.ids[0]);
			}

			if (IN.ids[1] < 0)
			{
				IN.ids[1] = -1;
			}
			else
			{
				IN.ids[1] = round(IN.ids[1]);
			}

			fixed4 textureColor = { 0.0f, 0.0f, 0.0f, 1.0f };

			if (IN.ids[0] < 0) {
				if (ShowUntextured > 0) {
					textureColor = float4(0.67f, 0.67f, 0.67f, 1.f);
				}
				else {
					discard;
				}
			}
			else {
				if (IN.ids[1] < 0) {
					float2 uv = computeUV(ArrayToMat4(ColorExtrinsics, IN.ids[0]), ArrayToMat3(ColorIntrinsics, IN.ids[0]), IN.vert);
					textureColor = float4(findTex(IN.ids[0], uv).bgr, 1.f);

				}
				else if (IN.weights[0] > 0.0f) {
					float2 uv1 = computeUV(ArrayToMat4(ColorExtrinsics, IN.ids[0]), ArrayToMat3(ColorIntrinsics, IN.ids[0]), IN.vert);
					float3 color1 = findTex(IN.ids[0], uv1).bgr;

					float2 uv2 = computeUV(ArrayToMat4(ColorExtrinsics, IN.ids[1]), ArrayToMat3(ColorIntrinsics, IN.ids[1]), IN.vert);
					float3 color2 = findTex(IN.ids[1], uv2).bgr;

					if (IN.weights[1] < WeightCutoff) {
						IN.weights[1] = 0.f;
						IN.weights[0] = 1.f;
					}
					textureColor = float4(IN.weights[0] * color1 + (1 - IN.weights[0]) * color2, 1.0f);

				}
				else {
					discard;
				}
			}

			return textureColor;
		}

		ENDCG
	}
	}
}
