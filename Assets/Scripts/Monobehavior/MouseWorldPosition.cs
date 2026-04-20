using UnityEngine;

public class MouseWorldPosition : Singleton<MouseWorldPosition>
{
    public Vector3 GetPosition() 
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane plane = new(Vector3.up, Vector3.zero);

        if(plane.Raycast(mouseCameraRay, out float distance)) { return mouseCameraRay.GetPoint(distance); }
        else { return Vector3.zero; }
    }
}