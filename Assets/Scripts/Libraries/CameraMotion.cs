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

        camera.position = targetPosition; // ensure exact final position
    }
}
