using UnityEngine;

public static class Utilities
{
    public static Vector3 GetMouseWorldPosition(float groundLevel = 0f) 
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new(Vector3.up, new Vector3(0, groundLevel, 0));

        if(plane.Raycast(mouseCameraRay, out float distance)) 
            return mouseCameraRay.GetPoint(distance);
        else 
            return Vector3.zero;
    }
}