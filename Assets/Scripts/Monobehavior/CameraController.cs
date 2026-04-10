using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CameraConfig camCFG;

    private Vector2 moveInput;
    private float turnInput;
    private Vector2 zoomInput;
    private bool isPressedLeftShift;

    private GameControls gameControls;

    #region InputSystemSetup
    private void OnEnable()
    {
        gameControls.Player.Enable();

        gameControls.Player.CameraMovement.performed += OnCameraMovePerformed;
        gameControls.Player.CameraMovement.canceled += OnCameraMoveCanceled;

        gameControls.Player.CameraTurn.performed += OnCameraTurnPerformed;
        gameControls.Player.CameraTurn.canceled += OnCameraTurnCanceled;

        gameControls.Player.CameraZoom.performed += OnCameraZoomPerformed;
        gameControls.Player.CameraZoom.canceled += OnCameraZoomCanceled;

        gameControls.Player.SpeedBoost.performed += OnSpeedBoostPreformed;
        gameControls.Player.SpeedBoost.canceled += OnSpeedBoostCanceled;
    }

    private void OnDisable()
    {
        gameControls.Player.CameraMovement.performed -= OnCameraMovePerformed;
        gameControls.Player.CameraMovement.canceled -= OnCameraMoveCanceled;

        gameControls.Player.CameraTurn.performed -= OnCameraTurnPerformed;
        gameControls.Player.CameraTurn.canceled -= OnCameraTurnCanceled;

        gameControls.Player.CameraZoom.performed -= OnCameraZoomPerformed;
        gameControls.Player.CameraZoom.canceled -= OnCameraZoomCanceled;

        gameControls.Player.SpeedBoost.performed -= OnSpeedBoostPreformed;
        gameControls.Player.SpeedBoost.canceled -= OnSpeedBoostCanceled;

        gameControls.Player.Disable();
    }

    private void OnCameraMovePerformed(InputAction.CallbackContext context) 
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnCameraMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnCameraTurnPerformed(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<float>();
    }

    private void OnCameraTurnCanceled(InputAction.CallbackContext context)
    {
        turnInput = 0;
    }

    private void OnCameraZoomPerformed(InputAction.CallbackContext context) 
    {
        zoomInput = context.ReadValue<Vector2>();   
    }

    private void OnCameraZoomCanceled(InputAction.CallbackContext context)
    {
        zoomInput = Vector2.zero ;
    }

    private void OnSpeedBoostPreformed(InputAction.CallbackContext context) 
    {
        isPressedLeftShift = true;
    }

    private void OnSpeedBoostCanceled(InputAction.CallbackContext context)
    {
        isPressedLeftShift = false;
    }
    #endregion

    private void Awake()
    {
        gameControls = new GameControls();
    }

    void Update()
    {
        if (moveInput != Vector2.zero) MoveCamera();
        
        if (turnInput != 0) RotateCamera();

        if (zoomInput != Vector2.zero) ZoomCamera();
    }

    public void MoveCamera() 
    {
        float movementAmount = camCFG.cameraMovementSpeed * Time.deltaTime * Mathf.Sqrt(transform.localScale.x);
        if (isPressedLeftShift) 
        {
            movementAmount *= 3;   
        }

        Vector3 movementDirectionLS = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 movementDirectionWS = transform.TransformDirection(movementDirectionLS);

        transform.position += movementAmount * movementDirectionWS;

        float moveClampX = Mathf.Clamp(transform.position.x, -camCFG.cameraMovementLimit.x, camCFG.cameraMovementLimit.x);
        float moveClampZ = Mathf.Clamp(transform.position.z, -camCFG.cameraMovementLimit.y, camCFG.cameraMovementLimit.y);
        transform.position = new Vector3(moveClampX, 0, moveClampZ);
    }

    private void ZoomCamera()
    {
        float zoomAmount = zoomInput.y * camCFG.cameraZoomSpeed * Time.deltaTime * transform.localScale.x;
        transform.localScale -= new Vector3(zoomAmount, zoomAmount, zoomAmount);

        float zoomClamp = Mathf.Clamp(transform.localScale.x, camCFG.cameraZoomLimit.x, camCFG.cameraZoomLimit.y);
        transform.localScale = new Vector3(zoomClamp, zoomClamp, zoomClamp);
    }

    public void RotateCamera()
    {
        float turnAmount = turnInput * camCFG.cameraTurnSpeed * Time.deltaTime;
        transform.Rotate(new Vector3(0, -turnAmount, 0));
    }
}
