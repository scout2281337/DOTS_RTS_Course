using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ZombieSpawnerAuthoring : MonoBehaviour
{
    public float timerMax;
    public float waveDelayMax;
    public int waveAmount;
    public int currentWave;
    public int amountToSpawn;
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;
    public Transform zombieSpawnTransform;
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
                timerMax = authoring.timerMax,
                waveAmount = authoring.waveAmount,
                currentWave = authoring.currentWave,
                waveDelayMax = authoring.waveDelayMax,
                waveDelay = authoring.waveDelayMax,
                amountToSpawn = authoring.amountToSpawn,
                randomWalkingDistanceMax = authoring.randomWalkingDistanceMax,
                randomWalkingDistanceMin = authoring.randomWalkingDistanceMin,
                zombieSpawnPosition = authoring.zombieSpawnTransform.position,
                isRandomWalkingOnStart = authoring.isRandomWalkingOnStart,
                startTargetEntity = GetEntity(authoring.startTargetGameObject, TransformUsageFlags.Dynamic),
            }) ;
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
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;
    public float3 zombieSpawnPosition;
    public bool isRandomWalkingOnStart;

    public Entity startTargetEntity;

}
