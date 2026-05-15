using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ZombieSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesReferences))
            return;

        EntityCommandBuffer entityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRW<ZombieSpawner> zombieSpawner, DynamicBuffer<DirectorEnemyEntryElement> enemyEntries, DynamicBuffer<SpawnPointElement> spawnPoints, RefRO<LocalTransform> localTransform) in
                 SystemAPI.Query<RefRW<ZombieSpawner>, DynamicBuffer<DirectorEnemyEntryElement>, DynamicBuffer<SpawnPointElement>, RefRO<LocalTransform>>())
        {
            if (zombieSpawner.ValueRO.currentWave > zombieSpawner.ValueRO.waveAmount)
                continue;

            float dt = SystemAPI.Time.DeltaTime;
            zombieSpawner.ValueRW.timer -= dt;

            if (zombieSpawner.ValueRO.waveActive)
            {
                zombieSpawner.ValueRW.waveTimer += dt;
                float income = EvaluateDirectorIncome(zombieSpawner.ValueRO);
                zombieSpawner.ValueRW.directorPoints = math.min(
                    zombieSpawner.ValueRO.directorBankCap,
                    zombieSpawner.ValueRO.directorPoints + income * dt);

                if (zombieSpawner.ValueRO.timer <= 0f && TrySelectEnemyToSpawn(ref zombieSpawner.ValueRW, enemyEntries, entitiesReferences.zombiePrefabEntity, out var enemyPrefab, out var enemyCost))
                {
                    SpawnEnemy(state.EntityManager, entityCommandBuffer, ref zombieSpawner.ValueRW, spawnPoints, localTransform.ValueRO, enemyPrefab);
                    zombieSpawner.ValueRW.directorPoints -= enemyCost;
                    zombieSpawner.ValueRW.spawnedEntities += 1;
                    zombieSpawner.ValueRW.timer = zombieSpawner.ValueRO.timerMax;
                }

                if (zombieSpawner.ValueRO.waveTimer >= zombieSpawner.ValueRO.waveDuration)
                {
                    zombieSpawner.ValueRW.waveActive = false;
                    zombieSpawner.ValueRW.waveDelay = zombieSpawner.ValueRO.waveDelayMax;
                }
            }
            else
            {
                bool spentRemainingBudget = false;
                if (zombieSpawner.ValueRO.timer <= 0f && TrySelectEnemyToSpawn(ref zombieSpawner.ValueRW, enemyEntries, entitiesReferences.zombiePrefabEntity, out var enemyPrefab, out var enemyCost))
                {
                    SpawnEnemy(state.EntityManager, entityCommandBuffer, ref zombieSpawner.ValueRW, spawnPoints, localTransform.ValueRO, enemyPrefab);
                    zombieSpawner.ValueRW.directorPoints -= enemyCost;
                    zombieSpawner.ValueRW.spawnedEntities += 1;
                    zombieSpawner.ValueRW.timer = zombieSpawner.ValueRO.timerMax;
                    spentRemainingBudget = true;
                }

                if (spentRemainingBudget)
                    continue;

                zombieSpawner.ValueRW.waveDelay -= dt;
                if (zombieSpawner.ValueRO.waveDelay <= 0f)
                {
                    int nextWave = zombieSpawner.ValueRO.currentWave + 1;
                    zombieSpawner.ValueRW.currentWave = nextWave;
                    zombieSpawner.ValueRW.waveActive = nextWave <= zombieSpawner.ValueRO.waveAmount;
                    zombieSpawner.ValueRW.waveDelay = zombieSpawner.ValueRO.waveDelayMax;
                    zombieSpawner.ValueRW.waveTimer = 0f;
                    zombieSpawner.ValueRW.spawnedEntities = 0;
                    zombieSpawner.ValueRW.directorPoints = math.min(
                        zombieSpawner.ValueRO.directorBankCap,
                        zombieSpawner.ValueRO.directorPoints + math.max(0f, zombieSpawner.ValueRO.amountToSpawn));
                    zombieSpawner.ValueRW.timer = 0f;
                }
            }
        }
    }

    private static float EvaluateDirectorIncome(in ZombieSpawner zombieSpawner)
    {
        float normalizedTime = zombieSpawner.waveDuration > 0f
            ? math.saturate(zombieSpawner.waveTimer / zombieSpawner.waveDuration)
            : 1f;

        float curve = EvaluateIntensityCurve(normalizedTime, zombieSpawner.startIntensity, zombieSpawner.peakTimeNormalized);
        float peakIncome = zombieSpawner.basePointsPerSecond + math.max(0, zombieSpawner.currentWave - 1) * zombieSpawner.pointsPerSecondPerWave;
        return peakIncome * curve;
    }

    private static float EvaluateIntensityCurve(float t, float startIntensity, float peakTimeNormalized)
    {
        if (t <= peakTimeNormalized)
        {
            float riseT = peakTimeNormalized > 0f ? t / peakTimeNormalized : 1f;
            return math.lerp(startIntensity, 1f, riseT);
        }

        float fallDuration = math.max(0.0001f, 1f - peakTimeNormalized);
        float fallT = (t - peakTimeNormalized) / fallDuration;
        return math.lerp(1f, 0f, fallT);
    }

    private static bool TrySelectEnemyToSpawn(ref ZombieSpawner zombieSpawner, DynamicBuffer<DirectorEnemyEntryElement> enemyEntries, Entity fallbackPrefab, out Entity enemyPrefab, out float enemyCost)
    {
        enemyPrefab = Entity.Null;
        enemyCost = 0f;

        float totalWeight = 0f;
        float cheapestCost = float.MaxValue;

        if (enemyEntries.Length == 0)
        {
            if (zombieSpawner.directorPoints < 1f || fallbackPrefab == Entity.Null)
                return false;

            enemyPrefab = fallbackPrefab;
            enemyCost = 1f;
            return true;
        }

        for (int i = 0; i < enemyEntries.Length; i++)
        {
            var entry = enemyEntries[i];
            if (entry.enemyPrefab == Entity.Null)
                continue;
            if (entry.unlockWave > zombieSpawner.currentWave)
                continue;
            if (entry.cost > zombieSpawner.directorPoints)
                continue;

            cheapestCost = math.min(cheapestCost, entry.cost);
            totalWeight += math.max(0.01f, entry.weight);
        }

        if (totalWeight <= 0f || cheapestCost == float.MaxValue)
            return false;

        float roll = zombieSpawner.random.NextFloat(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < enemyEntries.Length; i++)
        {
            var entry = enemyEntries[i];
            if (entry.enemyPrefab == Entity.Null)
                continue;
            if (entry.unlockWave > zombieSpawner.currentWave)
                continue;
            if (entry.cost > zombieSpawner.directorPoints)
                continue;

            cumulative += math.max(0.01f, entry.weight);
            if (roll <= cumulative)
            {
                enemyPrefab = entry.enemyPrefab;
                enemyCost = entry.cost;
                return true;
            }
        }

        return false;
    }

    private static void SpawnEnemy(EntityManager entityManager, EntityCommandBuffer entityCommandBuffer, ref ZombieSpawner zombieSpawner, DynamicBuffer<SpawnPointElement> spawnPoints, in LocalTransform spawnerTransform, Entity enemyPrefab)
    {
        Entity zombieEntity = entityCommandBuffer.Instantiate(enemyPrefab);
        entityCommandBuffer.SetComponent(zombieEntity, LocalTransform.FromPosition(GetSpawnPosition(ref zombieSpawner, spawnPoints)));
        EnsureFogRevealable(entityManager, entityCommandBuffer, zombieEntity, enemyPrefab);

        if (zombieSpawner.isRandomWalkingOnStart)
        {
            uint randomSeed = zombieSpawner.random.NextUInt(1u, uint.MaxValue);
            entityCommandBuffer.AddComponent(zombieEntity, new RandomWalking
            {
                originPosition = spawnerTransform.Position,
                targetPosition = spawnerTransform.Position,
                distanceMin = zombieSpawner.randomWalkingDistanceMin,
                distanceMax = zombieSpawner.randomWalkingDistanceMax,
                random = new Unity.Mathematics.Random(randomSeed),
            });
        }
        else if (zombieSpawner.startTargetEntity != Entity.Null)
        {
            entityCommandBuffer.SetComponent(zombieEntity, new Target
            {
                targetEntity = zombieSpawner.startTargetEntity,
            });
        }
    }

    private static float3 GetSpawnPosition(ref ZombieSpawner zombieSpawner, DynamicBuffer<SpawnPointElement> spawnPoints)
    {
        float3 basePosition = zombieSpawner.zombieSpawnPosition;
        if (spawnPoints.Length > 0)
        {
            int spawnPointIndex = zombieSpawner.random.NextInt(0, spawnPoints.Length);
            basePosition = spawnPoints[spawnPointIndex].position;
        }

        if (zombieSpawner.spawnRadius <= 0f)
            return basePosition;

        float angle = zombieSpawner.random.NextFloat(0f, math.PI * 2f);
        float distance = math.sqrt(zombieSpawner.random.NextFloat()) * zombieSpawner.spawnRadius;
        float2 offset = new float2(math.cos(angle), math.sin(angle)) * distance;
        return new float3(basePosition.x + offset.x, basePosition.y, basePosition.z + offset.y);
    }

    private static void EnsureFogRevealable(EntityManager entityManager, EntityCommandBuffer entityCommandBuffer, Entity entity, Entity prefab)
    {
        if (!entityManager.HasComponent<FogRevealable>(prefab))
            entityCommandBuffer.AddComponent<FogRevealable>(entity);

        if (!entityManager.HasComponent<FogVisible>(prefab))
            entityCommandBuffer.AddComponent<FogVisible>(entity);

        entityCommandBuffer.SetComponentEnabled<FogVisible>(entity, false);
    }
}
