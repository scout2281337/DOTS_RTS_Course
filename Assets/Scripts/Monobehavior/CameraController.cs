using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float cameraMovementSpeed;
    public float scrollMovementSpeed;

    public float sensitivity = 5f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    private float yaw = 0f;
    private float pitch = 0f;

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
        RotateCamera();
    }

    public void MoveCamera() 
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        transform.position += new Vector3(horizontalInput * cameraMovementSpeed, 0f, verticalInput * cameraMovementSpeed) * Time.deltaTime;


        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * Time.deltaTime * scrollMovementSpeed * scrollInput;
    }

    public void RotateCamera() 
    {
        if (Input.GetMouseButton(2) ) 
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * sensitivity;
            pitch -= mouseY * sensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
