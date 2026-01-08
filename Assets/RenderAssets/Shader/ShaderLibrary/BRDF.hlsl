#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04
#define BRDF_EPSILON 1e-4
#define MIN_PERCEPTUALROUGHNESS 0.045

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
    float perceptualRoughness;
    //float fresnel;
};

float ApplySpecularAA(float perceptualRoughness, float3 geomNormal, float3 shadingNormal)
{
    float variance = saturate(1.0 - dot(normalize(geomNormal), normalize(shadingNormal)));
    perceptualRoughness = saturate(perceptualRoughness + variance * 0.75);      // Scale down to control strength, avoid fireflies
    return perceptualRoughness;
}

float OneMinusReflectivity(float metallic)
{
	float range = 1.0 - MIN_REFLECTIVITY;
	return range * (1.0 - metallic);
}

float CustomPerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return max(perceptualRoughness * perceptualRoughness, BRDF_EPSILON);
}

float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * Pow4(1.0 - cosTheta);
}

float3 FresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
{
    float3 oneMinusR = (1.0 - roughness).xxx;
    float3 F90 = max(oneMinusR, F0);
    return F0 + (F90 - F0) * Pow4(1.0 - cosTheta);
}

float CustomD_GGX(float NdotH, float alpha)
{
    float a2 = alpha * alpha;
    float d = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / max(PI * d * d, BRDF_EPSILON);
}

float G_SmithGGXCorrelated(float NdotV, float NdotL, float alpha)
{
    float a2 = alpha * alpha;

    float gv = NdotL * sqrt(max(NdotV * (NdotV - NdotV * a2) + a2, BRDF_EPSILON));
    float gl = NdotV * sqrt(max(NdotL * (NdotL - NdotL * a2) + a2, BRDF_EPSILON));
    return 0.5 / max(gv + gl, BRDF_EPSILON);
}

BRDF GetBRDF(inout Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;

    brdf.perceptualRoughness = saturate(1.0 - surface.smoothness);
    brdf.perceptualRoughness = ApplySpecularAA(brdf.perceptualRoughness, surface.interpolatedNormal, surface.normal);
    brdf.perceptualRoughness = max(brdf.perceptualRoughness, MIN_PERCEPTUALROUGHNESS);
    brdf.roughness = CustomPerceptualRoughnessToRoughness(brdf.perceptualRoughness);

    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }

    brdf.specular = lerp(MIN_REFLECTIVITY.xxx, surface.color, surface.metallic);

    return brdf;
}

float3 SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 N = surface.normal;
    float3 V = surface.viewDirection;
    float3 L = light.direction;

    float3 H = normalize(L + V);

    float NdotV = saturate(dot(N, V));
    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));
    float VdotH = saturate(dot(V, H));

    float alpha = brdf.roughness;

    float D = CustomD_GGX(NdotH, alpha);
    float G = G_SmithGGXCorrelated(NdotV, NdotL, alpha);
    float3 F = FresnelSchlick(VdotH, brdf.specular);

    return min((D * G) * F / max(4.0 * NdotV * NdotL, BRDF_EPSILON), 50.0);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return brdf.diffuse + SpecularStrength(surface, brdf, light);
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
    float NdotV = saturate(dot(surface.normal, surface.viewDirection));
    float3 F = FresnelSchlickRoughness(NdotV, brdf.specular, brdf.perceptualRoughness);
    float3 kD = 1.0 - F;

    float3 diffuseTerm = diffuse * brdf.diffuse * kD;
    float3 specularTerm = specular * F;

    return (diffuseTerm + specularTerm) * surface.occlusion;
}

#endif