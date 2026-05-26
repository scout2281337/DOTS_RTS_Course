using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Header("Intensity")]
    [SerializeField] private float _intensityDeviation = 0.35f;
    private float _baseIntensity;

    [Header("Range")]
    [SerializeField] private bool _isRangeFlickering;
    [SerializeField] private float _rangeDeviation = 1.5f;
    private float _baseRange;

    [Header("Timing")]
    [SerializeField] private float _flickerSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float _smoothing = 0.35f;

    [Header("Periodic Flicker")]
    [SerializeField] private bool _isPeriodic;
    [SerializeField] private float _averageFlickeringInterval = 1.25f;
    [SerializeField] private float _flickeringIntervalDeviation = 0.5f;
    [SerializeField] private float _averageNonFlickeringInterval = 3f;
    [SerializeField] private float _nonFlickeringIntervalDeviation = 1f;

    private Light _targetLight;
    private float _noiseOffset;
    private bool _isCurrentlyFlickering = true;
    private float _nextIntervalSwitchTime;

    private void Awake()
    {
        _targetLight = GetComponent<Light>();
        _noiseOffset = Random.Range(0f, 1000f);

        _baseIntensity = _targetLight.intensity;
        _baseRange = _targetLight.range;

        _isCurrentlyFlickering = !_isPeriodic || Random.value > 0.5f;
        _nextIntervalSwitchTime = Time.time + GetCurrentIntervalDuration();
    }

    private void Update()
    {
        if (_isPeriodic)
            UpdatePeriodicState();

        if (!_isPeriodic || _isCurrentlyFlickering)
        {
            UpdateFlicker();
            return;
        }

        UpdateSteadyLight();
    }

    private void UpdatePeriodicState()
    {
        if (Time.time < _nextIntervalSwitchTime) return;

        _isCurrentlyFlickering = !_isCurrentlyFlickering;
        _nextIntervalSwitchTime = Time.time + GetCurrentIntervalDuration();
    }

    private float GetCurrentIntervalDuration()
    {
        float average = _isCurrentlyFlickering ? _averageFlickeringInterval : _averageNonFlickeringInterval;
        float deviation = _isCurrentlyFlickering ? _flickeringIntervalDeviation : _nonFlickeringIntervalDeviation;

        return Random.Range(average - deviation, average + deviation);
    }

    private void UpdateFlicker()
    {
        float noise = Mathf.PerlinNoise(_noiseOffset, Time.time * _flickerSpeed);
        float flicker = (noise * 2f) - 1f;
        float blend = 1f - _smoothing;

        float targetIntensity = Mathf.Max(0f, _baseIntensity + (flicker * _intensityDeviation));
        _targetLight.intensity = Mathf.Lerp(_targetLight.intensity, targetIntensity, blend);

        if (!_isRangeFlickering) return;

        float targetRange = Mathf.Max(0f, _baseRange + (flicker * _rangeDeviation));
        _targetLight.range = Mathf.Lerp(_targetLight.range, targetRange, blend);
    }

    private void UpdateSteadyLight()
    {
        float blend = 1f - _smoothing;
        _targetLight.intensity = Mathf.Lerp(_targetLight.intensity, _baseIntensity, blend);

        if (_isRangeFlickering)
            _targetLight.range = Mathf.Lerp(_targetLight.range, _baseRange, blend);
    }
}