// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ViveSR_Experience/toon" {
   Properties {
    _Color ("Diffuse Material Color", Color) = (1,1,1,1)
    _UnlitColor ("Unlit Color", Color) = (0.5,0.5,0.5,1)
    _DiffuseThreshold ("Lighting Threshold", Range(-1.1,1)) = 0.1   
	_StencilValue ("StencilRefValue", float) = 1
	[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp("Stencil Component", int) = 0	// disable
    }
   
     SubShader {
     Pass {
		Tags{ "LightMode" = "ForwardBase" }

		Stencil{
			Ref [_StencilValue]
			Comp[_StencilComp]
		}

        CGPROGRAM
       
        #pragma vertex vert
        #pragma fragment frag
       
        uniform float4 _Color;
        uniform float4 _UnlitColor;
        uniform float _DiffuseThreshold;
        uniform float4 _SpecColor = float4(1, 1, 1, 1);

        struct vertexInput {
           
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 texcoord : TEXCOORD0;
        };

        struct vertexOutput {
            float4 pos : SV_POSITION;
            float3 normalDir : TEXCOORD1;
            float4 lightDir : TEXCOORD2;
            float3 viewDir : TEXCOORD3;
            float2 uv : TEXCOORD0;
        };
       
        vertexOutput vert(vertexInput input)
        {
            vertexOutput output;
           
            //normal Vector
            output.normalDir = normalize ( mul( float4( input.normal, 0.0 ), unity_WorldToObject).xyz );
           
            //World Space P
            float4 WorldSP = mul(unity_ObjectToWorld, input.vertex);
           
            //view Vector
            output.viewDir = normalize( _WorldSpaceCameraPos.xyz - WorldSP.xyz ); //vector from object to the camera
           
            //light Vector
            float3 fTL = ( _WorldSpaceCameraPos.xyz - WorldSP.xyz);
            output.lightDir = float4(
                normalize( lerp(_WorldSpaceLightPos0.xyz , fTL, _WorldSpaceLightPos0.w) ),
                lerp(1.0 , 1.0/length(fTL), _WorldSpaceLightPos0.w)
            );
           
            //fragmentInput O;
            output.pos = UnityObjectToClipPos( input.vertex );  
           
            //UV Tex
            output.uv =input.texcoord;
           
            return output;
         
        }
       
        float4 frag(vertexOutput input) : COLOR
        {
 
			float nDotL = saturate(dot(input.normalDir, input.lightDir.xyz));
           
			//calculate diffuse cutoff
			float diffuseCutoff = saturate( ( max(_DiffuseThreshold, nDotL) - _DiffuseThreshold ) *1000 );
           
			//calculate specular cutoff
			float specularCutoff = saturate( max(1, dot(reflect(-input.lightDir.xyz, input.normalDir), input.viewDir))- 1) * 1000;
           
			float3 aL = (1-diffuseCutoff) * _UnlitColor.xyz;
			float3 diffuseR = (1-specularCutoff) * _Color.xyz * diffuseCutoff;
			float3 specularR = _SpecColor.xyz * specularCutoff;
       
			float3 combinedLight = (aL + diffuseR)  + specularR;
           
			return float4(combinedLight, 1.0); 
       
 
        }
       
        ENDCG
     
      }
 
   
   }
    Fallback "Diffuse"
}