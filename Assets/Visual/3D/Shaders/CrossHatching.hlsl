#ifndef CrossHatching_INCLUDED
#define CrossHatching_INCLUDED

float rand(float x)
{
    return frac(sin(x) * 43732.5453);
}

void SDFLine_float (float2 p, float2 lineStart, float2 lineEnd, float radius, out float sdf)
{
	float2 pa = p - lineStart;
	float2 ba = lineEnd - lineStart;
	float h = saturate( dot( pa, ba ) / dot( ba, ba ) );
	sdf = length( pa - ba * h ) - radius;
}

float Hatching(float2 uv, float amount, float length, float2 uvShift)
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
	SDFLine_float(aUV, lineStart, lineEnd, 0.0, SDFLines);
	
	return SDFLines;
}

void HatchingPattern_float(float2 uv, float amount, float width, float length, float smoothness, out float hatching)
{

	float SDFLines = min(
	Hatching(uv, amount - 1.0, length, float2(0.0, 0.0)), 
	Hatching(uv, amount - 1.0, length, float2(0.5, 0.5)));

	// Applying smoothstep with correctly working smoothness
	float smoothedLine = smoothstep(width / 5 - smoothness / 2,
					 				width / 5 + smoothness / 2 + 0.001,
					 				SDFLines);

	// One minus for intuitive width 
	hatching = 1 - smoothedLine;
}

void CrossHatchingDithering_float(float value, float2 uv, float width, float smoothness, out float pattern)
{
	float amount = 0.5 / width;

	float hatchingX;
	float hatchingY;
	HatchingPattern_float(uv.xy, amount, 0.7, 4, smoothness, hatchingX);
	HatchingPattern_float(uv.yx, amount, 0.7, 4, smoothness, hatchingY);

	float edge1 = step(0.01,1 - value) * hatchingX;
	float edge2 = step(0.5,1 - value) * hatchingY;
	float edge3 = step(0.99,1 - value);
	pattern = saturate(edge1 + edge2 + edge3);
}

#endif