using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ZombieSpawnerAuthoring : MonoBehaviour
{
    public float timerMax;
    public float waveDelayMax;
    public int waveAmount;
    public int amountToSpawn;
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;
    public Transform zombieSpawnTransform; 
    public class Baker : Baker<ZombieSpawnerAuthoring>
    {
        
        public override void Bake(ZombieSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ZombieSpawner
            {
                timerMax = authoring.timerMax,
                waveAmount = authoring.waveAmount,
                waveDelayMax = authoring.waveDelayMax,
                waveDelay = authoring.waveDelayMax,
                amountToSpawn = authoring.amountToSpawn,
                randomWalkingDistanceMax = authoring.randomWalkingDistanceMax,
                randomWalkingDistanceMin = authoring.randomWalkingDistanceMin,
                zombieSpawnPosition = authoring.zombieSpawnTransform.position
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
    public int amountToSpawn;
    public int spawnedEntities;
    public float randomWalkingDistanceMin;
    public float randomWalkingDistanceMax;
    public float3 zombieSpawnPosition;
}
