using System.Collections;
using UnityEngine;

public class ArtilleryStrikeDirector : Singleton<ArtilleryStrikeDirector>
{

    [Header("Strike")]
    [SerializeField, Min(0.1f)] private float radius = 5f;
    [SerializeField, Min(0.05f)] private float warningDelay = 3f;
    [SerializeField, Min(0f)] private float damage = 65f;
    [SerializeField] private bool useDamageFalloff = true;
    [SerializeField, Range(0f, 1f)] private float edgeDamageMultiplier = 0.45f;

    [Header("Barrage")]
    [SerializeField, Min(1)] private int shellsPerBarrage = 5;
    [SerializeField, Min(0f)] private float shellInterval = 0.35f;
    [SerializeField, Min(0f)] private float barrageScatterRadius = 12f;
    [SerializeField] private Vector2 warningDelayRandomOffset = new(-0.35f, 0.45f);

    [Header("Random Events")]
    [SerializeField] private bool autoRunRandomBarrage;
    [SerializeField] private bool useTransformAsRandomAreaCenter = true;
    [SerializeField] private Vector3 randomAreaCenter;
    [SerializeField] private Vector2 randomAreaSize = new(90f, 90f);
    [SerializeField] private Vector2 randomBarrageInterval = new(45f, 90f);

    [Header("Presentation")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0.1f)] private float explosionLifetime = 6f;
    [SerializeField] private AudioCueSO incomingAudioCue;
    [SerializeField] private AudioCueSO explosionAudioCue;
    [SerializeField] private bool shakeCameraOnImpact = true;
    [SerializeField] private string warningLabel = "ARTILLERY";

    private float randomTimer;

    private Vector3 RandomAreaOrigin => useTransformAsRandomAreaCenter ? transform.position : randomAreaCenter;



    private void OnEnable()
    {
        ResetRandomTimer();
    }



    private void Update()
    {
        if (!autoRunRandomBarrage)
            return;

        randomTimer -= Time.deltaTime;
        if (randomTimer > 0f)
            return;

        RequestRandomBarrage();
        ResetRandomTimer();
    }

    public static bool TryRequestStrike(Vector3 position)
    {
        if (Instance == null)
            return false;

        Instance.RequestStrike(position);
        return true;
    }

    public static bool TryRequestBarrage(Vector3 center)
    {
        if (Instance == null)
            return false;

        Instance.RequestBarrage(center);
        return true;
    }

    public void RequestStrike(Vector3 position)
    {
        RequestStrike(position, radius, warningDelay, damage, reportEvent: true);
    }

    public void RequestStrike(Vector3 position, float strikeRadius, float delay, float strikeDamage)
    {
        RequestStrike(position, strikeRadius, delay, strikeDamage, reportEvent: true);
    }

    public void RequestBarrage(Vector3 center)
    {
        StartCoroutine(SpawnBarrage(center));
    }

    public void RequestRandomBarrage()
    {
        RequestBarrage(GetRandomPointInArea());
    }

    [ContextMenu("Test Strike At Director")]
    private void TestStrikeAtDirector()
    {
        RequestStrike(transform.position);
    }

    [ContextMenu("Test Barrage At Director")]
    private void TestBarrageAtDirector()
    {
        RequestBarrage(transform.position);
    }

    private IEnumerator SpawnBarrage(Vector3 center)
    {
        ReportArtilleryEvent(center, Mathf.Max(radius, barrageScatterRadius), warningDelay + shellsPerBarrage * shellInterval);

        for (int i = 0; i < shellsPerBarrage; i++)
        {
            Vector2 offset = Random.insideUnitCircle * barrageScatterRadius;
            Vector3 position = center + new Vector3(offset.x, 0f, offset.y);
            float randomizedDelay = Mathf.Max(0.05f, warningDelay + Random.Range(warningDelayRandomOffset.x, warningDelayRandomOffset.y));

            RequestStrike(position, radius, randomizedDelay, damage, reportEvent: false);

            if (shellInterval > 0f)
                yield return new WaitForSeconds(shellInterval);
        }
    }

    private void RequestStrike(Vector3 position, float strikeRadius, float delay, float strikeDamage, bool reportEvent)
    {
        if (reportEvent)
            ReportArtilleryEvent(position, strikeRadius, delay);

        GameObject strikeObject = new("Artillery Strike");
        strikeObject.transform.position = position;

        ArtilleryStrikeZone strike = strikeObject.AddComponent<ArtilleryStrikeZone>();
        strike.Initialize(
            strikeRadius,
            delay,
            strikeDamage,
            edgeDamageMultiplier,
            useDamageFalloff,
            explosionPrefab,
            explosionLifetime,
            incomingAudioCue,
            explosionAudioCue,
            shakeCameraOnImpact);
    }

    private void ReportArtilleryEvent(Vector3 position, float eventRadius, float eventDuration)
    {
        WorldEventUtility.Report(
            WorldEventType.ArtilleryStrike,
            position,
            WorldEventImportance.High,
            WorldEventKnowledge.Exact,
            eventRadius,
            eventDuration,
            warningLabel);
    }

    private Vector3 GetRandomPointInArea()
    {
        Vector3 origin = RandomAreaOrigin;
        float halfX = Mathf.Max(0f, randomAreaSize.x) * 0.5f;
        float halfZ = Mathf.Max(0f, randomAreaSize.y) * 0.5f;

        return new Vector3(
            origin.x + Random.Range(-halfX, halfX),
            origin.y,
            origin.z + Random.Range(-halfZ, halfZ));
    }

    private void ResetRandomTimer()
    {
        float min = Mathf.Max(0.1f, Mathf.Min(randomBarrageInterval.x, randomBarrageInterval.y));
        float max = Mathf.Max(min, Mathf.Max(randomBarrageInterval.x, randomBarrageInterval.y));
        randomTimer = Random.Range(min, max);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.02f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);

        Vector3 origin = useTransformAsRandomAreaCenter ? transform.position : randomAreaCenter;
        Gizmos.color = new Color(1f, 0.25f, 0.02f, 0.16f);
        Gizmos.DrawCube(origin, new Vector3(randomAreaSize.x, 0.1f, randomAreaSize.y));
    }
}
