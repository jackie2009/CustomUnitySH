// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/Standard IBL" {
	Properties {
		 
	 
  
		 [Enum(unitySH,0,raddiancemap,1,irradiancemap,2)] _shMode("SH_MODE", int) = 0
	 
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard_M fullforwardshadows
		#include "UnityPBSLighting.cginc"
   
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#define PI  3.1415926
 
		samplerCUBE _radiancemap;
		samplerCUBE _irradiancemap;
	 
 
		int _shMode;
 

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

			struct Input {
			float4 color;
		};

		inline half4 LightingStandard_M(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
		{
			return float4(gi.indirect.diffuse,1);
		}
 
		inline void LightingStandard_M_GI(
			SurfaceOutputStandard s,
			UnityGIInput data,
			inout UnityGI gi)
		{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
			gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
			
#else
			Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
			 
			gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
			if (_shMode==1) {
 
					float3 normal = s.Normal;
					float3 tangent = float3(0, 1, 0);
					float upOrDown = dot(normal, tangent);

					if (upOrDown == 1)
						tangent = float3(1, 0, 0);
					else if (upOrDown == -1)
						tangent = float3(-1, 0, 0);
					else
						tangent = normalize(cross(float3(0, 1, 0), normal));


					float3 binormal = normalize(cross(normal, tangent));
					float sampleDelta = 0.25 / 32;
					int N1 = 0;
					int N2 = 0;
					float3 irradiance = float3(0, 0, 0);
					for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
					{
						N2 = 0;
						for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
						{
							// 在切线空间内，法线固定为(0, 1, 0)，然后生成采样向量，再通过TBN矩阵转换为世界空间方向
							float3 tangentSpaceNormal = float3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
							float3 worldNormal = tangentSpaceNormal.x * tangent + tangentSpaceNormal.y * binormal + tangentSpaceNormal.z * normal;
							float3 c = texCUBE(_radiancemap, normalize(worldNormal)).rgb * cos(theta) *  sin(theta);
							irradiance += c;
							N2++;
						}
						N1++;
					}
					float weight = PI * PI / (N1 * N2);
			 
                    irradiance *= weight;

					gi.indirect.diffuse =  irradiance/ PI;
				 
			}
			if (_shMode == 2) {
				gi.indirect.diffuse = texCUBE(_irradiancemap, normalize(s.Normal)).rgb / PI;
			}
#endif
		}

		void surf (Input IN,inout SurfaceOutputStandard o) {
			 
			o.Albedo =  1;
		 
		}
		ENDCG
	}
	FallBack "Diffuse"
}
