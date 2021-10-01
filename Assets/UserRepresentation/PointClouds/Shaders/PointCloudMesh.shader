Shader "Entropy/PointCloudMesh"{
    Properties{
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
        _PointSizeFactor("Point Size multiply", Float) = 1.0
        _MainTex("Texture", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
    }

    SubShader {
        // This subshader needs geometry, converts points to quads which are then sampled
        // off a texture. Works on recent OpenGL and DX GPUs.
        Lighting Off
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Tags {
            "Queue" = "AlphaTest" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent"
        }

        Pass {
        CGPROGRAM
                #pragma target 4.0
                #pragma vertex Vertex
                #pragma geometry Geometry
                #pragma fragment Fragment
                #pragma multi_compile _UNITY_COLORSPACE_GAMMA

                #include "UnityCG.cginc"
                

                half4       _Tint;
                half        _PointSize;
                half        _PointSizeFactor;
                sampler2D	_MainTex;
                fixed       _Cutoff;

                struct appdata
                {
                    float4  vertex : POSITION;
                    half3   color : COLOR;
                };

                struct v2f {
                    float4  position : SV_Position;
                    half4   color : COLOR;
                    half2	uv : TEXCOORD0;
					float  size : PSIZE;
                };

                v2f Vertex(appdata v) {
                    v2f o;
                    o.position = UnityObjectToClipPos(v.vertex);

                    half4 col = half4(v.color.rgb, _Tint.a);
#if UNITY_COLORSPACE_GAMMA
                    col.rgb *= _Tint.rgb * 2;
#else
                    col.rgb *= LinearToGammaSpace(_Tint) * 2;
                    col.rgb = GammaToLinearSpace(col);
#endif
                    o.color = col;
                    o.uv = half2(0.5, 0.5);
					o.size = _PointSize*_PointSizeFactor;
                    return o;
                }

                [maxvertexcount(4)]
                void Geometry(point v2f input[1], inout TriangleStream<v2f> outStream) {
                    float4 origin = input[0].position;
                    float2 extent = abs(UNITY_MATRIX_P._11_22 * input[0].size);
#if SHADER_API_GLCORE || SHADER_API_METAL
                    extent.x *= -1;
#endif
                    // Copy the basic information.
                    v2f o = input[0];
                    o.position.xzw = origin.xzw;

                    // Bottom-Left side vertex
                    o.position.x = origin.x + extent.x;
                    o.position.y = origin.y + extent.y;
                    o.uv = half2(1, 1);
                    outStream.Append(o);

                    // Up-Left vertex
                    o.position.x = origin.x - extent.x;
                    o.position.y = origin.y + extent.y;
                    o.uv = half2(0, 1);
                    outStream.Append(o);

                    // Up-Right side vertex
                    o.position.x = origin.x + extent.x;
                    o.position.y = origin.y - extent.y;
                    o.uv = half2(1, 0);
                    outStream.Append(o);

                    // Bottom-Right vertex
                    o.position.x = origin.x - extent.x;
                    o.position.y = origin.y - extent.y;
                    o.uv = half2(0, 0);
                    outStream.Append(o);

                    outStream.RestartStrip();
                }

      
                half4 Fragment(v2f input) : SV_Target {
                    half4 tc = tex2D(_MainTex, input.uv);
                    half4 c = input.color;
                    c.a *= tc.a;
                    clip(tc.a < _Cutoff ? -1 : 1);
                    return c;
                }
                ENDCG
            }
    }

    SubShader {
        // This subshader does not use geometry processing, but renders square points. Therefore
        // it works on older OpenGL and DX GPUs, but most importantly it works on Metal (which does
        // not support geometry processing)
        Lighting Off
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Tags {
            "Queue" = "AlphaTest" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent"
        }

        Pass {
        CGPROGRAM
                #pragma vertex Vertex
                #pragma fragment Fragment
                #pragma multi_compile _UNITY_COLORSPACE_GAMMA

                #include "UnityCG.cginc"
                

                half4       _Tint;
                half        _PointSize;
                half        _PointSizeFactor;
                fixed       _Cutoff;

                struct appdata
                {
                    float4  vertex : POSITION;
                    half3   color : COLOR;
                };

                struct v2f {
                    float4  position : SV_Position;
                    half4   color : COLOR;
                    float    size : PSIZE;
                };

                v2f Vertex(appdata v) {
                    v2f o;
                    o.position = UnityObjectToClipPos(v.vertex);

                    half4 col = half4(v.color.rgb, _Tint.a);
#if UNITY_COLORSPACE_GAMMA
                    col.rgb *= _Tint.rgb * 2;
#else
                    col.rgb *= LinearToGammaSpace(_Tint) * 2;
                    col.rgb = GammaToLinearSpace(col);
#endif
                    o.color = col;
                    //
                    // xxxjack I think this computation is wrong. Undoutedly I can get the
                    // correct information from the various matrices but I don't know how.
                    //
                    float pixelsPerMeter = _ScreenParams.y / o.position.w;
                    o.size = _PointSize * _PointSizeFactor * pixelsPerMeter;
                    return o;
                }
      
                half4 Fragment(v2f input) : SV_Target {
                    half4 c = input.color;
                    clip(c.a < _Cutoff ? -1 : 1);
                    return c;
                }
                ENDCG
            }
    }
}