#ifndef SDFFunctions_INCLUDED
#define SDFFunctions_INCLUDED

void SDFLine_float (float2 p, float2 lineStart, float2 lineEnd, float radius, out float sdf)
{
	float2 pa = p - lineStart;
	float2 ba = lineEnd - lineStart;
	float h = saturate( dot( pa, ba ) / dot( ba, ba ) );
	sdf = length( pa - ba * h ) - radius;
}


#endif