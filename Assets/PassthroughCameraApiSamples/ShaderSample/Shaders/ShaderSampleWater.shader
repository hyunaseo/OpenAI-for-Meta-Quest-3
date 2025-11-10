Shader "Meta/PCA/ShaderSampleWater" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _DetailMap ("Texture", 2D) = "white" {}
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _NormalOffsetX ("Normal Offset X", Float) = 0.01
        _NormalOffsetY ("Normal Offset Y", Float) = 0.01
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ReflectIntensity ("Reflection Intensity", Float) = 1.0
    }

    SubShader {
        Tags {"RenderType"="Opaque"}
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NormalMap;
            float4 _NormalMap_ST;

            sampler2D _DetailMap;
            float4 _DetailMap_ST;

            float _WaveAmplitude;
            float _NormalOffsetX;
            float _NormalOffsetY;

            fixed4 _Color;

            float _ReflectIntensity;

            v2f vert (appdata v) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.uv;

                // Control waves speed
                float nX = ((_NormalOffsetX * _SinTime.y));
                float nY = ((_NormalOffsetY * _SinTime.y));
                float3 normal = tex2D(_NormalMap, i.uv + float2(nX,nY)).rgb;

                // Set the wave distorsion
                float2 distortedUV = uv + normal.xy * (_WaveAmplitude/100);
                float2 detailDistortedUV = i.uv + normal.xy * (_WaveAmplitude/100);

                // Mirror the texture
                distortedUV.y = 1 - distortedUV.y;

                // Set the color
                fixed4 reflectionCol = tex2D(_MainTex, distortedUV) * _ReflectIntensity;
                fixed4 col = reflectionCol * tex2D(_DetailMap,detailDistortedUV) * _Color;
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
