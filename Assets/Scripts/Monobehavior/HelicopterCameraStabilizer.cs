using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class HelicopterCameraStabilizer : MonoBehaviour
{
    private enum PositionStabilizationMode
    {
        LockToMount,
        SmoothVerticalOnly,
        SmoothAllAxes
    }

    [Header("Mount")]
    [SerializeField] private Transform _mountOverride;
    [SerializeField] private bool _snapOnEnable = true;

    [Header("Position")]
    [SerializeField] private PositionStabilizationMode _positionMode = PositionStabilizationMode.SmoothVerticalOnly;
    [SerializeField, Min(0f)] private float _verticalSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float _maxVerticalOffset = 0.18f;
    [SerializeField, Min(0f)] private float _fullPositionSmoothTime = 0.035f;
    [SerializeField, Min(0f)] private float _maxPositionOffset = 0.12f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float _yawFollowSpeed = 18f;
    [SerializeField, Min(0f)] private float _tiltFollowSpeed = 5f;

    private Transform _capturedParent;
    private Vector3 _capturedLocalPosition;
    private Quaternion _capturedLocalRotation;

    private Vector3 _smoothedPosition;
    private Vector3 _positionVelocity;
    private float _verticalVelocity;
    private Quaternion _smoothedYaw;
    private Quaternion _smoothedTilt;
    private bool _hasCapturedPose;
    private bool _isInitialized;

    private void Awake()
    {
        CaptureCurrentPoseAsMount();
    }

    private void OnEnable()
    {
        if (!_hasCapturedPose)
        {
            CaptureCurrentPoseAsMount();
        }

        if (_snapOnEnable)
        {
            SnapToMount();
        }
    }

    private void LateUpdate()
    {
        if (!TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation))
        {
            return;
        }

        if (!_isInitialized)
        {
            SnapToPose(desiredPosition, desiredRotation);
            return;
        }

        transform.position = StabilizePosition(desiredPosition);
        transform.rotation = StabilizeRotation(desiredRotation);
    }

    [ContextMenu("Capture Current Pose As Mount")]
    public void CaptureCurrentPoseAsMount()
    {
        _capturedParent = transform.parent;
        _capturedLocalPosition = transform.localPosition;
        _capturedLocalRotation = transform.localRotation;
        _hasCapturedPose = true;
    }

    [ContextMenu("Snap To Mount")]
    public void SnapToMount()
    {
        if (TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation))
        {
            SnapToPose(desiredPosition, desiredRotation);
        }
    }

    private bool TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        if (_mountOverride != null)
        {
            desiredPosition = _mountOverride.position;
            desiredRotation = _mountOverride.rotation;
            return true;
        }

        if (_capturedParent == null)
        {
            desiredPosition = transform.position;
            desiredRotation = transform.rotation;
            return false;
        }

        desiredPosition = _capturedParent.TransformPoint(_capturedLocalPosition);
        desiredRotation = _capturedParent.rotation * _capturedLocalRotation;
        return true;
    }

    private void SnapToPose(Vector3 desiredPosition, Quaternion desiredRotation)
    {
        _smoothedPosition = desiredPosition;
        _positionVelocity = Vector3.zero;
        _verticalVelocity = 0f;

        _smoothedYaw = ExtractYaw(desiredRotation, Vector3.up, Quaternion.identity);
        _smoothedTilt = Quaternion.Inverse(_smoothedYaw) * desiredRotation;

        transform.position = desiredPosition;
        transform.rotation = desiredRotation;
        _isInitialized = true;
    }

    private Vector3 StabilizePosition(Vector3 desiredPosition)
    {
        switch (_positionMode)
        {
            case PositionStabilizationMode.LockToMount:
                _smoothedPosition = desiredPosition;
                _positionVelocity = Vector3.zero;
                _verticalVelocity = 0f;
                break;

            case PositionStabilizationMode.SmoothVerticalOnly:
                float smoothedY = Mathf.SmoothDamp(
                    _smoothedPosition.y,
                    desiredPosition.y,
                    ref _verticalVelocity,
                    _verticalSmoothTime);

                smoothedY = Mathf.Clamp(
                    smoothedY,
                    desiredPosition.y - _maxVerticalOffset,
                    desiredPosition.y + _maxVerticalOffset);

                _smoothedPosition = new Vector3(desiredPosition.x, smoothedY, desiredPosition.z);
                _positionVelocity = Vector3.zero;
                break;

            case PositionStabilizationMode.SmoothAllAxes:
                _smoothedPosition = Vector3.SmoothDamp(
                    _smoothedPosition,
                    desiredPosition,
                    ref _positionVelocity,
                    _fullPositionSmoothTime);

                Vector3 positionOffset = Vector3.ClampMagnitude(
                    _smoothedPosition - desiredPosition,
                    _maxPositionOffset);

                _smoothedPosition = desiredPosition + positionOffset;
                _verticalVelocity = 0f;
                break;
        }

        return _smoothedPosition;
    }

    private Quaternion StabilizeRotation(Quaternion desiredRotation)
    {
        float yawBlend = GetExponentialBlend(_yawFollowSpeed);
        float tiltBlend = GetExponentialBlend(_tiltFollowSpeed);

        Quaternion desiredYaw = ExtractYaw(desiredRotation, Vector3.up, _smoothedYaw);
        Quaternion desiredTilt = Quaternion.Inverse(desiredYaw) * desiredRotation;

        _smoothedYaw = Quaternion.Slerp(_smoothedYaw, desiredYaw, yawBlend);
        _smoothedTilt = Quaternion.Slerp(_smoothedTilt, desiredTilt, tiltBlend);

        return _smoothedYaw * _smoothedTilt;
    }

    private static float GetExponentialBlend(float speed)
    {
        if (speed <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-speed * Time.deltaTime);
    }

    private static Quaternion ExtractYaw(Quaternion rotation, Vector3 up, Quaternion fallback)
    {
        Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, up);

        if (forward.sqrMagnitude < 0.0001f)
        {
            return fallback;
        }

        return Quaternion.LookRotation(forward.normalized, up);
    }
}
