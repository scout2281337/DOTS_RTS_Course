using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CameraConfig _camCFG;

    private Vector2 _moveInput;
    private float _turnInput;
    private Vector2 _zoomInput;
    private bool _isPressedLeftShift;

    private GameControls _gameControls;

    #region InputSystemSetup
    private void OnEnable()
    {
        _gameControls.Player.Enable();

        _gameControls.Player.CameraMovement.performed += OnCameraMovePerformed;
        _gameControls.Player.CameraMovement.canceled += OnCameraMoveCanceled;

        _gameControls.Player.CameraTurn.performed += OnCameraTurnPerformed;
        _gameControls.Player.CameraTurn.canceled += OnCameraTurnCanceled;

        _gameControls.Player.CameraZoom.performed += OnCameraZoomPerformed;
        _gameControls.Player.CameraZoom.canceled += OnCameraZoomCanceled;

        _gameControls.Player.SpeedBoost.performed += OnSpeedBoostPreformed;
        _gameControls.Player.SpeedBoost.canceled += OnSpeedBoostCanceled;
    }

    private void OnDisable()
    {
        _gameControls.Player.CameraMovement.performed -= OnCameraMovePerformed;
        _gameControls.Player.CameraMovement.canceled -= OnCameraMoveCanceled;

        _gameControls.Player.CameraTurn.performed -= OnCameraTurnPerformed;
        _gameControls.Player.CameraTurn.canceled -= OnCameraTurnCanceled;

        _gameControls.Player.CameraZoom.performed -= OnCameraZoomPerformed;
        _gameControls.Player.CameraZoom.canceled -= OnCameraZoomCanceled;

        _gameControls.Player.SpeedBoost.performed -= OnSpeedBoostPreformed;
        _gameControls.Player.SpeedBoost.canceled -= OnSpeedBoostCanceled;

        _gameControls.Player.Disable();
    }

    private void OnCameraMovePerformed(InputAction.CallbackContext context) 
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnCameraMoveCanceled(InputAction.CallbackContext context)
    {
        _moveInput = Vector2.zero;
    }

    private void OnCameraTurnPerformed(InputAction.CallbackContext context)
    {
        _turnInput = context.ReadValue<float>();
    }

    private void OnCameraTurnCanceled(InputAction.CallbackContext context)
    {
        _turnInput = 0;
    }

    private void OnCameraZoomPerformed(InputAction.CallbackContext context) 
    {
        _zoomInput = context.ReadValue<Vector2>();   
    }

    private void OnCameraZoomCanceled(InputAction.CallbackContext context)
    {
        _zoomInput = Vector2.zero ;
    }

    private void OnSpeedBoostPreformed(InputAction.CallbackContext context) 
    {
        _isPressedLeftShift = true;
    }

    private void OnSpeedBoostCanceled(InputAction.CallbackContext context)
    {
        _isPressedLeftShift = false;
    }
    #endregion

    private void Awake()
    {
        _gameControls = new GameControls();
    }

    void Update()
    {
        if (_moveInput != Vector2.zero) MoveCamera();
        
        if (_turnInput != 0) RotateCamera();

        if (_zoomInput != Vector2.zero) ZoomCamera();
    }

    public void MoveCamera() 
    {
        float movementAmount = _camCFG.cameraMovementSpeed * Time.deltaTime * Mathf.Sqrt(transform.localScale.x);
        if (_isPressedLeftShift) 
        {
            movementAmount *= 3;   
        }

        Vector3 movementDirectionLS = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        Vector3 movementDirectionWS = transform.TransformDirection(movementDirectionLS);

        transform.position += movementAmount * movementDirectionWS;

        float moveClampX = Mathf.Clamp(transform.position.x, -_camCFG.cameraMovementLimit.x, _camCFG.cameraMovementLimit.x);
        float moveClampZ = Mathf.Clamp(transform.position.z, -_camCFG.cameraMovementLimit.y, _camCFG.cameraMovementLimit.y);
        transform.position = new Vector3(moveClampX, 0, moveClampZ);
    }

    private void ZoomCamera()
    {
        float zoomAmount = _zoomInput.y * _camCFG.cameraZoomSpeed * Time.deltaTime * transform.localScale.x;
        transform.localScale -= new Vector3(zoomAmount, zoomAmount, zoomAmount);

        float zoomClamp = Mathf.Clamp(transform.localScale.x, _camCFG.cameraZoomLimit.x, _camCFG.cameraZoomLimit.y);
        transform.localScale = new Vector3(zoomClamp, zoomClamp, zoomClamp);
    }

    public void RotateCamera()
    {
        float turnAmount = _turnInput * _camCFG.cameraTurnSpeed * Time.deltaTime;
        transform.Rotate(new Vector3(0, -turnAmount, 0));
    }
}
