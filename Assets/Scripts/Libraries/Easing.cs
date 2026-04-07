using UnityEngine;

public static class Easing
{
    // Sine
    public static float InSine(float t) => 1 - Mathf.Cos((t * Mathf.PI) / 2);
    public static float OutSine(float t) => Mathf.Sin((t * Mathf.PI) / 2);
    public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

    // Quadratic
    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1 - (1 - t) * (1 - t);
    public static float InOutQuad(float t) =>
        t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

    // Cubic
    public static float InCubic(float t) => t * t * t;
    public static float OutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
    public static float InOutCubic(float t) =>
        t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

    // Quartic
    public static float InQuart(float t) => t * t * t * t;
    public static float OutQuart(float t) => 1 - Mathf.Pow(1 - t, 4);
    public static float InOutQuart(float t) =>
        t < 0.5f ? 8 * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 4) / 2;

    // Quintic
    public static float InQuint(float t) => t * t * t * t * t;
    public static float OutQuint(float t) => 1 - Mathf.Pow(1 - t, 5);
    public static float InOutQuint(float t) =>
        t < 0.5f ? 16 * t * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 5) / 2;

    // Exponential
    public static float InExpo(float t) => t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
    public static float OutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
    public static float InOutExpo(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        return t < 0.5f
            ? Mathf.Pow(2, 20 * t - 10) / 2
            : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
    }

    // Circular
    public static float InCirc(float t) => 1 - Mathf.Sqrt(1 - t * t);
    public static float OutCirc(float t) => Mathf.Sqrt(1 - Mathf.Pow(t - 1, 2));
    public static float InOutCirc(float t) =>
        t < 0.5f
            ? (1 - Mathf.Sqrt(1 - 4 * t * t)) / 2
            : (Mathf.Sqrt(1 - Mathf.Pow(-2 * t + 2, 2)) + 1) / 2;

    // Bounce
    public static float InBounce(float t) => 1f - OutBounce(1f - t);

    public static float OutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    public static float InOutBounce(float t) =>
        t < 0.5f
            ? (1f - OutBounce(1f - 2f * t)) / 2f
            : (1f + OutBounce(2f * t - 1f)) / 2f;

    // Elastic
    public static float InElastic(float t)
    {
        if (t == 0f || t == 1f) return t;
        float c = (2f * Mathf.PI) / 0.3f;
        return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c);
    }

    public static float OutElastic(float t)
    {
        if (t == 0f || t == 1f) return t;
        float c = (2f * Mathf.PI) / 0.3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c) + 1f;
    }

    public static float InOutElastic(float t)
    {
        if (t == 0f || t == 1f) return t;
        float c = (2f * Mathf.PI) / 0.45f;
        if (t < 0.5f)
            return -0.5f * Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c);
        else
            return Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c) * 0.5f + 1f;
    }

    // Back
    public static float InBack(float t)
    {
        const float c1 = 1.70158f;
        return t * t * ((c1 + 1f) * t - c1);
    }

    public static float OutBack(float t)
    {
        const float c1 = 1.70158f;
        return 1f + (--t) * t * ((c1 + 1f) * t + c1);
    }

    public static float InOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;

        return t < 0.5f
            ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
            : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
    }
}
