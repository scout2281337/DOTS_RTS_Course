using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ZombieSpawnerAuthoring : MonoBehaviour
{
    [System.Serializable]
    public struct DirectorEnemyEntry
    {
        public GameObject enemyPrefab;
        public float cost;
        public float weight;
        public int unlockWave;
    }

    [Header("Director")]
    public float timerMax = 1.5f;
    public float waveDelayMax = 8f;
    public int waveAmount = 5;
    public int currentWave = 1;
    public int amountToSpawn;
    public float waveDuration = 45f;
    public float basePointsPerSecond = 1f;
    public float pointsPerSecondPerWave = 0.5f;
    [Range(0f, 1f)] public float startIntensity = 0.2f;
    [Range(0.1f, 0.95f)] public float peakTimeNormalized = 0.66f;
    public float directorBankCap = 30f;
    public float startingDirectorPoints = 0f;
    public DirectorEnemyEntry[] enemyEntries;

    [Header("Spawn Points")]
    public Transform zombieSpawnTransform;
    public Transform[] spawnPoints;

    [Header("Legacy")]
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;

    [Header("случайное передвижение врагов при спавне")]
    public bool isRandomWalkingOnStart;

    [Header("Обязательно при использовании не рандомного спавна")]
    public GameObject startTargetGameObject;

    public class Baker : Baker<ZombieSpawnerAuthoring>
    {
        public override void Bake(ZombieSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ZombieSpawner
            {
                timer = authoring.timerMax,
                timerMax = authoring.timerMax,
                waveAmount = math.max(1, authoring.waveAmount),
                currentWave = math.max(1, authoring.currentWave),
                waveDelayMax = math.max(0f, authoring.waveDelayMax),
                waveDelay = math.max(0f, authoring.waveDelayMax),
                amountToSpawn = authoring.amountToSpawn,
                spawnedEntities = 0,
                waveDuration = authoring.waveDuration > 0f ? authoring.waveDuration : 45f,
                waveTimer = 0f,
                basePointsPerSecond = authoring.basePointsPerSecond > 0f ? authoring.basePointsPerSecond : 1f,
                pointsPerSecondPerWave = math.max(0f, authoring.pointsPerSecondPerWave),
                startIntensity = math.clamp(authoring.startIntensity, 0f, 1f),
                peakTimeNormalized = math.clamp(authoring.peakTimeNormalized, 0.1f, 0.95f),
                directorPoints = math.max(0f, authoring.startingDirectorPoints),
                directorBankCap = authoring.directorBankCap > 0f ? authoring.directorBankCap : 30f,
                randomWalkingDistanceMax = authoring.randomWalkingDistanceMax,
                randomWalkingDistanceMin = authoring.randomWalkingDistanceMin,
                zombieSpawnPosition = authoring.zombieSpawnTransform != null ? authoring.zombieSpawnTransform.position : authoring.transform.position,
                isRandomWalkingOnStart = authoring.isRandomWalkingOnStart,
                startTargetEntity = authoring.startTargetGameObject != null
                    ? GetEntity(authoring.startTargetGameObject, TransformUsageFlags.Dynamic)
                    : Entity.Null,
                waveActive = true,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)(authoring.GetInstanceID() & int.MaxValue) + 1u)
            });

            var enemyBuffer = AddBuffer<DirectorEnemyEntryElement>(entity);
            if (authoring.enemyEntries != null)
            {
                foreach (var entry in authoring.enemyEntries)
                {
                    if (entry.enemyPrefab == null)
                        continue;

                    enemyBuffer.Add(new DirectorEnemyEntryElement
                    {
                        enemyPrefab = GetEntity(entry.enemyPrefab, TransformUsageFlags.Dynamic),
                        cost = math.max(0.1f, entry.cost),
                        weight = math.max(0.01f, entry.weight),
                        unlockWave = math.max(1, entry.unlockWave)
                    });
                }
            }

            var spawnPointBuffer = AddBuffer<SpawnPointElement>(entity);
            if (authoring.spawnPoints != null && authoring.spawnPoints.Length > 0)
            {
                for (int i = 0; i < authoring.spawnPoints.Length; i++)
                {
                    if (authoring.spawnPoints[i] == null)
                        continue;

                    spawnPointBuffer.Add(new SpawnPointElement
                    {
                        position = authoring.spawnPoints[i].position
                    });
                }
            }

            if (spawnPointBuffer.Length == 0)
            {
                spawnPointBuffer.Add(new SpawnPointElement
                {
                    position = authoring.zombieSpawnTransform != null
                        ? authoring.zombieSpawnTransform.position
                        : authoring.transform.position
                });
            }
        }
    }
}

public struct ZombieSpawner : IComponentData
{
    public float timer;
    public float timerMax;
    public float waveDelayMax;
    public float waveDelay;
    public int waveAmount;
    public int currentWave;
    public int amountToSpawn;
    public int spawnedEntities;
    public float waveDuration;
    public float waveTimer;
    public float basePointsPerSecond;
    public float pointsPerSecondPerWave;
    public float startIntensity;
    public float peakTimeNormalized;
    public float directorPoints;
    public float directorBankCap;
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;
    public float3 zombieSpawnPosition;
    public bool isRandomWalkingOnStart;
    public bool waveActive;
    public Entity startTargetEntity;
    public Unity.Mathematics.Random random;
}

public struct DirectorEnemyEntryElement : IBufferElementData
{
    public Entity enemyPrefab;
    public float cost;
    public float weight;
    public int unlockWave;
}

public struct SpawnPointElement : IBufferElementData
{
    public float3 position;
}
