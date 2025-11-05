using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private GameControls gameControls;
    
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private Vector2 scrollInput;

    public float cameraMovementSpeed;
    public float scrollMovementSpeed;

    public float sensitivity = 5f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    private float yaw = 0f;
    private float pitch = 0f;

    public GameObject mainCamera;

    private bool isRightMouseButtonPressed;
    private bool isPressedLeftShift;
    private void Awake()
    {
        gameControls = new GameControls();
        Vector3 angles = mainCamera.transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }


    private void OnEnable()
    {
        gameControls.Player.Enable();

        gameControls.Player.Movement.performed += OnMovePerformed;
        gameControls.Player.Movement.canceled += OnMoveCanceled;

        gameControls.Player.CameraRotation.performed += OnLookPerformed;
        gameControls.Player.CameraRotation.canceled += OnLookCanceled;

        gameControls.Player.ClickForRotation.performed += OnClickPerformed;
        gameControls.Player.ClickForRotation.canceled += OnClickCanceled;

        gameControls.Player.ScreenZoom.performed += OnScrollPerformed;
        gameControls.Player.ScreenZoom.canceled += OnScrollCanceled;

        gameControls.Player.SpeedBoost.performed += OnSpeedBoostPreformed;
        gameControls.Player.SpeedBoost.canceled += OnSpeedBoostCanceled;

    }

    private void OnDisable()
    {
        gameControls.Player.Movement.performed -= OnMovePerformed;
        gameControls.Player.Movement.canceled -= OnMoveCanceled;

        gameControls.Player.CameraRotation.performed -= OnLookPerformed;
        gameControls.Player.CameraRotation.canceled -= OnLookCanceled;

        gameControls.Player.ClickForRotation.performed -= OnClickPerformed;
        gameControls.Player.ClickForRotation.canceled -= OnClickCanceled;

        gameControls.Player.ScreenZoom.performed -= OnScrollPerformed;
        gameControls.Player.ScreenZoom.canceled -= OnScrollCanceled;

        gameControls.Player.SpeedBoost.performed -= OnSpeedBoostPreformed;
        gameControls.Player.SpeedBoost.canceled -= OnSpeedBoostCanceled;

        gameControls.Player.Disable();
    }
    private void OnMovePerformed(InputAction.CallbackContext context) 
    {
        moveInput = context.ReadValue<Vector2>();
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        mouseInput = context.ReadValue<Vector2>();
    }
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        mouseInput = Vector2.zero;
    }
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        isRightMouseButtonPressed = true;    
    }
    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        isRightMouseButtonPressed= false;
    }
    private void OnScrollPerformed(InputAction.CallbackContext context) 
    {
        scrollInput = context.ReadValue<Vector2>();   
    }
    private void OnScrollCanceled(InputAction.CallbackContext context)
    {
        scrollInput = Vector2.zero ;
    }

    private void OnSpeedBoostPreformed(InputAction.CallbackContext context) 
    {
        isPressedLeftShift = true;
    }
    private void OnSpeedBoostCanceled(InputAction.CallbackContext context)
    {
        isPressedLeftShift = false;
    }






    void Update()
    {
        MoveCamera();
        RotateCamera();
    }

    public void MoveCamera() 
    {
        float speedMyltiplier = 1;
        
        if (isPressedLeftShift) 
        {
            speedMyltiplier = 3;   
        }
        

        transform.position += ((transform.forward * moveInput.y) + (transform.right * moveInput.x)) * speedMyltiplier * cameraMovementSpeed * Time.deltaTime;


        transform.position += mainCamera.transform.forward * Time.deltaTime * scrollMovementSpeed * scrollInput.y;
    }

    public void RotateCamera() 
    {
        if (isRightMouseButtonPressed) 
        {
            yaw += mouseInput.x * sensitivity;
            pitch -= mouseInput.y * sensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            
            Vector3 cameraEuler = mainCamera.transform.eulerAngles;
            transform.rotation = Quaternion.Euler( 0f, cameraEuler.y, 0f); 
        }
    }
    
    
}
