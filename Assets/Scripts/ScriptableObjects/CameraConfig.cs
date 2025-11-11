using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "Scriptable Objects/GameControls/CameraConfig")]
public class CameraConfig : ScriptableObject
{
    public float cameraMovementSpeed;
    public float cameraTurnSpeed;
    public float cameraZoomSpeed;

    public Vector2 cameraMovementLimit;
    public Vector2 cameraZoomLimit;
}
