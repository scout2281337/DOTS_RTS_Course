using UnityEngine;
using System.Collections;

public static class CameraMotion
{
    public static IEnumerator MoveCameraToPoint(Vector3 targetPosition, float duration)
    {
        Transform camera = Camera.main.transform;
        Vector3 startPosition = camera.position;
        float time = 0f; 

        while (time < duration)
        {
            float progress = Easing.InOutCubic(time);
            camera.position = Vector3.Lerp(startPosition, targetPosition, progress);

            time += Time.deltaTime / duration; // Making so that time goes from 0 to 1
            yield return null;
        }

        camera.position = targetPosition; // Ensuring exact final position
    }

    public static Vector3 GetShakeOffset(float time, float frequency, Vector3 amplitude, float seed)
    {
        return new Vector3(
            GetNoise(time, frequency, seed + 0.11f) * amplitude.x,
            GetNoise(time, frequency, seed + 1.37f) * amplitude.y,
            GetNoise(time, frequency, seed + 2.71f) * amplitude.z);
    }

    private static float GetNoise(float time, float frequency, float seed)
    {
        return (Mathf.PerlinNoise(seed, time * frequency) - 0.5f) * 2f;
    }
}
