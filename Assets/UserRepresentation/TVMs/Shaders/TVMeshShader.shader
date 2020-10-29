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
		uniform float4x4 ColorIntrinsics[MAX_NUMBER_OF_CAMERAS];
		uniform float4x4 ColorExtrinsics[MAX_NUMBER_OF_CAMERAS];
		uniform float RadialDistortionCoeffs[MAX_NUMBER_OF_CAMERAS * 6];
		uniform float TangentialDistortionCoeffs[MAX_NUMBER_OF_CAMERAS * 2];
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
					result[i][j] = arr[j + i * 3 + 9 * camId]; // column major as a result of Eigen used in the C++ DLL
			return result;
		}

		float3 getRadialP1(float arr[48], int camId)
		{
			return float3(arr[camId * 6], arr[camId * 6 + 1], arr[camId * 6 + 2]);
		}

		float3 getRadialP2(float arr[48], int camId)
		{
			return float3(arr[camId * 6 + 3], arr[camId * 6 + 4], arr[camId * 6 + 5]);
		}

		float2 getTangential(float arr[16], int camId)
		{
			return float2(arr[camId * 2], arr[camId * 2 + 1]);
		}

		float2 undistortUV(float3 p3d, float3 color_radial_P1, float3 color_radial_P2, float2 color_tangential, int valid, float4x4 color_intrinsics)
		{
			float2 homogeneous = p3d.xy / p3d.z;
			float2 homogeneous_sq = homogeneous * homogeneous;
			float homogeneous_xy = homogeneous.x * homogeneous.y;
			float radius_sq = homogeneous_sq.x + homogeneous_sq.y;
			if (radius_sq > (1.7 * 1.7)) {
				valid = 0;
			}
			float3 radius_vec = float3(radius_sq,
				radius_sq * radius_sq,
				radius_sq * radius_sq * radius_sq
			);
			float a = 1.0 + dot(color_radial_P1, radius_vec);
			float b = 1.0 + dot(color_radial_P2, radius_vec);
			b = b != 0.0 ? 1.0 / b : 1.0;
			float d = a * b;
			float2 homogeneous_dist = homogeneous * d;
			float2 radius_sq_dist = 2.0 * homogeneous_sq + radius_vec.xx;
			homogeneous_dist += (radius_sq_dist * color_tangential.yx) + (homogeneous_xy * color_tangential);
			valid = 1;
			float4 color_focal_principal = float4(color_intrinsics[0][0], color_intrinsics[1][1], color_intrinsics[2][0], color_intrinsics[2][1]);
			return homogeneous_dist * color_focal_principal.xy + color_focal_principal.zw;
		}

		float2 computeUV(float4x4 invCamExtrinsics, float4x4 camIntrinsics, float3 pos, int camId)
		{
			float3 proj = mul(invCamExtrinsics, float4(pos, 1.0f)).xyz;
			int valid = 0;
			float2 homogeneous_dist = undistortUV(proj, getRadialP1(RadialDistortionCoeffs, camId), getRadialP2(RadialDistortionCoeffs, camId), getTangential(TangentialDistortionCoeffs, camId), valid, camIntrinsics);
			float3 undistorted_vec3 = float3(homogeneous_dist.x, homogeneous_dist.y, 1.0f);
			return float2 (undistorted_vec3.x / float(TextureWidth), undistorted_vec3.y / float(TextureHeight));
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
					textureColor = float4(0.0f, 0.0f, 0.0f, 1.f);
				}
				else {
					discard;
				}
			}
			else {
				if (IN.ids[1] < 0) {
					float2 uv = computeUV((ColorExtrinsics[IN.ids[0]]), (ColorIntrinsics[IN.ids[0]]), IN.vert, IN.ids[0]);
					textureColor = float4(findTex(IN.ids[0], uv).bgr, 1.f);

				}
				else if (IN.weights[0] > 0.0f) {
					float2 uv1 = computeUV((ColorExtrinsics[IN.ids[0]]), (ColorIntrinsics[IN.ids[0]]), IN.vert, IN.ids[0]);
					float3 color1 = findTex(IN.ids[0], uv1).bgr;

					float2 uv2 = computeUV((ColorExtrinsics[IN.ids[1]]), (ColorIntrinsics[IN.ids[1]]), IN.vert, IN.ids[1]);
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
