using UnityEngine;
using System.Collections.Generic;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private Vector3 _defaultPositionAmplitude = new (0.08f, 0.08f, 0.04f);
    [SerializeField] private Vector3 _defaultRotationAmplitude = new (0.8f, 0.8f, 1.5f);
    [SerializeField] private float _defaultFrequency = 18f;
    [SerializeField] private bool _playContinuousShakeOnEnable;
    [SerializeField] private float _continuousBlendSpeed = 3f;

    private readonly List<ShakeInstance> _activeShakes = new();

    private float _continuousWeight;
    private float _continuousTargetWeight;
    private float _noiseSeed;
    private Vector3 _lastPositionOffset;
    private Vector3 _lastRotationOffset;

    public void PlayShake(float duration, Vector3 positionAmplitude, Vector3 rotationAmplitude, float frequency)
    {
        if (duration <= 0f || frequency <= 0f)
        {
            return;
        }

        // Each one-shot shake keeps its own timing and random seed so multiple
        // impacts can overlap without looking identical.
        _activeShakes.Add(new ShakeInstance
        {
            StartTime = Time.time,
            Duration = duration,
            Frequency = frequency,
            PositionAmplitude = positionAmplitude,
            RotationAmplitude = rotationAmplitude,
            Seed = Random.Range(0f, 1000f)
        });
    }

    public void StartContinuousShake()
    {
        _continuousTargetWeight = 1f;
    }

    public void StopContinuousShake()
    {
        _continuousTargetWeight = 0f;
    }

    public void SetContinuousShakeStrength(float strength)
    {
        _continuousTargetWeight = Mathf.Clamp01(strength);
    }

    private void RemovePreviousOffset()
    {
        // Undo only the shake applied by this component on the previous frame.
        // This lets the camera's normal movement update cleanly without drift.
        transform.localPosition -= _lastPositionOffset;
        transform.localRotation *= Quaternion.Inverse(Quaternion.Euler(_lastRotationOffset));

        _lastPositionOffset = Vector3.zero;
        _lastRotationOffset = Vector3.zero;
    }


    private struct ShakeInstance
    {
        public float StartTime;
        public float Duration;
        public float Frequency;
        public Vector3 PositionAmplitude;
        public Vector3 RotationAmplitude;
        public float Seed;
    }


    private void Awake()
    {
        _noiseSeed = Random.Range(0f, 1000f);
    }

    private void LateUpdate()
    {
        // Remove old shake first, then compute and apply the new shake for this frame.
        RemovePreviousOffset();

        _continuousWeight = Mathf.MoveTowards(
            _continuousWeight,
            _continuousTargetWeight,
            _continuousBlendSpeed * Time.deltaTime);

        Vector3 positionOffset = Vector3.zero;
        Vector3 rotationOffset = Vector3.zero;

        if (_continuousWeight > 0f)
        {
            // Continuous shake is for sustained vibration like helicopter flight
            // or engine rumble, and it blends in/out using continuousWeight.
            positionOffset += CameraMotion.GetShakeOffset(Time.time, _defaultFrequency, _defaultPositionAmplitude, _noiseSeed) * _continuousWeight;
            rotationOffset += CameraMotion.GetShakeOffset(Time.time, _defaultFrequency, _defaultRotationAmplitude, _noiseSeed + 10f) * _continuousWeight;
        }

        for (int i = _activeShakes.Count - 1; i >= 0; i--)
        {
            ShakeInstance shake = _activeShakes[i];
            float elapsed = Time.time - shake.StartTime;

            if (elapsed >= shake.Duration)
            {
                _activeShakes.RemoveAt(i);
                continue;
            }

            float normalizedTime = elapsed / shake.Duration;
            // Fade each impulse out smoothly so hits feel natural instead of snapping off.
            float strength = 1f - Easing.OutQuad(normalizedTime);

            positionOffset += CameraMotion.GetShakeOffset(Time.time, shake.Frequency, shake.PositionAmplitude, shake.Seed) * strength;
            rotationOffset += CameraMotion.GetShakeOffset(Time.time, shake.Frequency, shake.RotationAmplitude, shake.Seed + 10f) * strength;
        }

        // Apply the combined local-space shake on top of the camera's base transform.
        transform.localPosition += positionOffset;
        transform.localRotation *= Quaternion.Euler(rotationOffset);

        // Cache the applied offsets so they can be removed exactly next frame.
        _lastPositionOffset = positionOffset;
        _lastRotationOffset = rotationOffset;
    }

    private void OnEnable()
    {
        _continuousTargetWeight = _playContinuousShakeOnEnable ? 1f : 0f;
    }

    private void OnDisable()
    {
        RemovePreviousOffset();
        _activeShakes.Clear();
        _continuousWeight = 0f;
    }
}
