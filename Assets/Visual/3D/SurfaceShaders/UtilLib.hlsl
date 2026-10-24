#ifndef UtilLib_INCLUDED
#define UtilLib_INCLUDED

float randFloat (float x)
{
    return frac(sin(x) * 43732.5453);
}

#endif