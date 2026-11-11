Shader "Custom/Terrain"
{
    Properties
	{
		_RockInnerShallow ("Rock Inner Shallow", Color) = (1,1,1,1)
		_RockInnerDeep ("Rock Inner Deep", Color) = (1,1,1,1)
		_RockLight ("Rock Light", Color) = (1,1,1,1)
		_RockDark ("Rock Dark", Color) = (1,1,1,1)
		_RockLight2 ("Rock Light2", Color) = (1,1,1,1)
		_RockDark2 ("Rock Dark2", Color) = (1,1,1,1)
		_RockLight3 ("Rock Light3", Color) = (1,1,1,1)
		_RockDark3 ("Rock Dark3", Color) = (1,1,1,1)
		_GrassLight ("Grass Light", Color) = (1,1,1,1)
		_GrassDark ("Grass Dark", Color) = (1,1,1,1)


		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Test ("Test", Float) = 0.0

		_NoiseTex("Noise Texture", 2D) = "White" {}
		_TerrainTypeTex("Terrain Type Texture", 2D) = "white" {}


	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0


		sampler2D _MainTex;
		sampler3D DensityTex;
		sampler2D _NoiseTex;
		sampler2D _TerrainTypeTex;

		struct Input
		{
			float2 uv_MainTex;
			float3 worldNormal;
			float3 worldPos;
		};

		//<float> terrainTypes;

		half _Glossiness;
		fixed4 _Color;
		float4 _RockInnerShallow;
		float4 _RockInnerDeep;
		float4 _RockLight;
		float4 _RockDark;
		float4 _RockLight2;
		float4 _RockDark2;
		float4 _RockLight3;
		float4 _RockDark3;
		float4 _GrassLight;
		float4 _GrassDark;
		float _Test;
		float planetBoundsSize;

		float oceanRadius;

		//float3 mod289(float3 x){
		//	return x - floor(x * (1.0 / 289.0)) * 289.0;
		//}
		//
		//float4 mod289(float4 x){
		//	return x - floor(x * (1.0 / 289.0)) * 289.0;
		//}
		//
		//float4 permute(float4 x){
		//	return mod289(((x * 34.0) + 10.0) * x);
		//}
		//
		//float4 taylorInvSqrt(float4 r){
		//	return 1.79284291400159 - 0.85373472095314 * r;
		//}
		//
		//float snoise(float3 v){
		//	const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
		//	const float4 D = float4(0.0, 0.5, 1.0, 2.0);
		//	
		//	// First corner
		//	float3 i = floor(v + dot(v, C.yyy));
		//	float3 x0 = v - i + dot(i, C.xxx);
		//	
		//	// Other corners
		//	float3 g = step(x0.yzx, x0.xyz);
		//	float3 l = 1.0 - g;
		//	float3 i1 = min(g.xyz, l.zxy);
		//	float3 i2 = max(g.xyz, l.zxy);
		//	
		//	//   x0 = x0 - 0.0 + 0.0 * C.xxx;
		//	//   x1 = x0 - i1  + 1.0 * C.xxx;
		//	//   x2 = x0 - i2  + 2.0 * C.xxx;
		//	//   x3 = x0 - 1.0 + 3.0 * C.xxx;
		//	float3 x1 = x0 - i1 + C.xxx;
		//	float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
		//	float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y
		//	
		//	// Permutations
		//	i = mod289(i);
		//	float4 p = permute(permute(permute(
		//	         i.z + float4(0.0, i1.z, i2.z, 1.0))
		//	       + i.y + float4(0.0, i1.y, i2.y, 1.0))
		//	       + i.x + float4(0.0, i1.x, i2.x, 1.0));
		//	
		//	// Gradients: 7x7 points over a square, mapped onto an octahedron.
		//	// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
		//	float n_ = 0.142857142857; // 1.0/7.0
		//	float3 ns = n_ * D.wyz - D.xzx;
		//	
		//	float4 j = p - 49.0 * floor(p * ns.z * ns.z); //  mod(p,7*7)
		//	
		//	float4 x_ = floor(j * ns.z);
		//	float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
		//	
		//	float4 x = x_ * ns.x + ns.yyyy;
		//	float4 y = y_ * ns.x + ns.yyyy;
		//	float4 h = 1.0 - abs(x) - abs(y);
		//	
		//	float4 b0 = float4(x.xy, y.xy);
		//	float4 b1 = float4(x.zw, y.zw);
		//	
		//	//float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
		//	//float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
		//	float4 s0 = floor(b0) * 2.0 + 1.0;
		//	float4 s1 = floor(b1) * 2.0 + 1.0;
		//	float4 sh = -step(h, float4(0.0, 0.0, 0.0, 0.0));
		//	
		//	float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
		//	float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
		//	
		//	float3 p0 = float3(a0.xy, h.x);
		//	float3 p1 = float3(a0.zw, h.y);
		//	float3 p2 = float3(a1.xy, h.z);
		//	float3 p3 = float3(a1.zw, h.w);
		//	
		//	//Normalise gradients
		//	float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
		//	p0 *= norm.x;
		//	p1 *= norm.y;
		//	p2 *= norm.z;
		//	p3 *= norm.w;
		//	
		//	// Mix final noise value
		//	float4 m = max(0.5 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
		//	m = m * m;
		//	return 105.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
		//}

		float4 triplanarOffset(float3 vertPos, float3 normal, float3 scale, sampler2D tex, float2 offset) {
			float3 scaledPos = vertPos / scale;
			float4 colX = tex2D (tex, scaledPos.zy + offset);
			float4 colY = tex2D(tex, scaledPos.xz + offset);
			float4 colZ = tex2D (tex,scaledPos.xy + offset);
			
			// Square normal to make all values positive + increase blend sharpness
			float3 blendWeight = normal * normal;
			// Divide blend weight by the sum of its components. This will make x + y + z = 1
			blendWeight /= dot(blendWeight, 1);
			return colX * blendWeight.x + colY * blendWeight.y + colZ * blendWeight.z;
		}

		float3 worldToTexPos(float3 worldPos) {
			return worldPos / planetBoundsSize + 0.5;
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{

			float3 t = worldToTexPos(IN.worldPos);
			float density = tex3D(DensityTex, t);
			// 0 = flat, 0.5 = vertical, 1 = flat (but upside down)
			float steepness = 1 - (dot(float3(0,1,0), IN.worldNormal) * 0.5 + 0.5);
			float dstFromCentre = length(IN.worldPos);

			float4 noise = triplanarOffset(IN.worldPos, IN.worldNormal, 60, _NoiseTex, 0);
			float4 noise2 = triplanarOffset(IN.worldPos, IN.worldNormal, 50, _NoiseTex, 0);
			//float angle01 = dot(normalize(IN.worldPos), IN.worldNormal) * 0.5 + 0.5;
			//o.Albedo = lerp(float3(1,0,0), float3(0,1,0), smoothstep(0.4,0.6,angle01));

			float metallic = 0;
			float rockMetalStrength = 0.4;
			
			float threshold = 0.005;
			if (density < -threshold) {
				float rockDepthT = saturate(abs(density + threshold) * 20);
				o.Albedo = lerp(_RockInnerShallow, _RockInnerDeep, rockDepthT);
				metallic = lerp(rockMetalStrength, 1, rockDepthT);
			}
			else {
				float4 grassCol = lerp(_GrassLight, _GrassDark, noise.r);
				int r = 10;
				float4 rockCol = lerp(_RockLight, _RockDark, (int)(noise2.r*r) / float(r));
				float4 rockCol2 = lerp(_RockLight2, _RockDark2, (int)(noise2.r*r) / float(r));
				float4 rockCol3 = lerp(_RockLight3, _RockDark3, (int)(noise2.r*r) / float(r));
				float n = _Test * noise.r * 0.5;//(noise.r-0.1) * _Test;

				float rockWeight = smoothstep(0.24 + n, 0.24 + 0.1 + n, steepness);
				//rockWeight += tex2D(_TerrainTypeTex, IN.uv_MainTex).r;

				if (IN.uv_MainTex.y >= 0.89999){
					o.Albedo = lerp(rockCol, lerp(grassCol, rockCol, rockWeight), (IN.uv_MainTex.y - 0.9) * 10);

					if (IN.uv_MainTex.y <= 0.901){
						o.Albedo = rockCol;
					}
					//o.Albedo = steepness > _Test;
					//metallic = lerp(rockMetalStrength, lerp(0, rockMetalStrength, rockWeight), 1 - (IN.uv_MainTex.y - 0.98) * 50);

					//o.Albedo = steepness > _Test;
					//metallic = rockMetalStrength;
				}
				else if (IN.uv_MainTex.y >= 0.69999){
					o.Albedo = lerp(rockCol2, rockCol, (IN.uv_MainTex.y - 0.7) * 5);
					metallic = lerp(0.1, 0, (IN.uv_MainTex.y - 0.7) * 5);

					if (IN.uv_MainTex.y <= 0.7){
						o.Albedo = rockCol2;
						metallic = 0.1;
					}
					//metallic = lerp(rockMetalStrength, lerp(0, rockMetalStrength, rockWeight), (IN.uv_MainTex.y - 0.9) * 10);
				}
				else if (IN.uv_MainTex.y >= 0.29999){
					o.Albedo = lerp(rockCol3, rockCol2, (IN.uv_MainTex.y - 0.3) * 2.5);
					metallic = lerp(0.2, 0.1, (IN.uv_MainTex.y - 0.3) * 2.5);

					if (IN.uv_MainTex.y <= 0.3){
						o.Albedo = rockCol3;
						metallic = 0.2;
					}
					//metallic = lerp(rockMetalStrength, lerp(0, rockMetalStrength, rockWeight), (IN.uv_MainTex.y - 0.9) * 10);
				}
				else if (IN.uv_MainTex.y >= -0.00001){
					o.Albedo = float4(0,0,0,1);
					
					metallic = 0.8;
					_Glossiness = 0.1;
				}

			}

			//o.Albedo = dstFromCentre > oceanRadius;
			
			//o.Albedo = tex2D(_TerrainTypeTex, IN.uv_MainTex);
			
			//o.Albedo = metallic;

			o.Metallic = metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
