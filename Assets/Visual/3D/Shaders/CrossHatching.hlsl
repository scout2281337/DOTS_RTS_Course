#ifndef CrossHatching_INCLUDED
#define CrossHatching_INCLUDED

#include "Assets/Visual/3D/Shaders/SDFFunctions.hlsl"

float rand(float x)
{
    return frac(sin(x) * 43732.5453);
}

float HatchingSDF(float2 uv, float amount, float width, float length, float smoothness, float2 uvShift)
{
	float2 aUV = amount * uv;
	aUV.y /= length;
	aUV += uvShift;
	float2 flooredUV = floor(aUV);
	
	float randNumForTile = rand(floor(aUV.x) + 10.0 * floor(aUV.y));
	float2 randShift = (float2(randNumForTile, rand(randNumForTile)) - 0.5) * 0.25;
	
	float SDFLines;
	float2 lineStart = float2(0.5 + flooredUV.x + randShift.x,
							  0.8 + flooredUV.y);
	float2 lineEnd = float2(0.5 + flooredUV.x + randShift.y,
							0.2 + flooredUV.y);
	float thinning = (1 - frac(aUV.y));
	SDFLine_float(aUV, lineStart, lineEnd, thinning * 0.05, SDFLines);
	
	return SDFLines;
}

void HatchingPattern_float(float2 uv, float amount, float width, float length, float smoothness, out float hatching)
{

	float SDFLines = min(
	HatchingSDF(uv, amount - 1.0, width, length, smoothness, float2(0.0, 0.0)), 
	HatchingSDF(uv, amount - 1.0, width, length, smoothness, float2(0.5, 0.5)));

	// Applying smoothstep with correctly working smoothness
	float smoothedLine = smoothstep((width / 5 - smoothness / 2),
					 				(width / 5 + smoothness / 2) + 0.001,
					 				SDFLines);
	// One minus for intuitive width 
	hatching = 1 - smoothedLine;
}

void CrossHatchingDithering_float(float value, float2 uv, float width, float3 shadowEdges, float smoothness, out float pattern)
{
	float amount = 0.6 / width;

	float hatchingX;
	float hatchingY;
	HatchingPattern_float(uv.xy, amount, 0.6, 5, smoothness, hatchingX);
	HatchingPattern_float(uv.yx, amount, 0.7, 5, smoothness, hatchingY);
	
    float mod1 = saturate(pow((1 - value) * 2 - shadowEdges.x, 3));
    float mod2 = saturate(pow((1 - value) * 2 - shadowEdges.y, 3));
    float edge1 = (1 - value) * hatchingX * mod1;
    float edge2 = (1 - value) * hatchingY * mod2;
    float edge3 = step(shadowEdges.z, 1 - value);
	
    pattern = saturate(edge1 + edge2 + edge3);
}
#endif