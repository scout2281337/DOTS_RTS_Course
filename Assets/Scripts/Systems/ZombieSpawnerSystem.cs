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

        foreach ((RefRW<ZombieSpawner> zombieSpawner, DynamicBuffer<DirectorEnemyEntryElement> enemyEntries, RefRO<LocalTransform> localTransform) in
                 SystemAPI.Query<RefRW<ZombieSpawner>, DynamicBuffer<DirectorEnemyEntryElement>, RefRO<LocalTransform>>())
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
                    SpawnEnemy(state.EntityManager, entityCommandBuffer, zombieSpawner.ValueRO, localTransform.ValueRO, enemyPrefab);
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
                    SpawnEnemy(state.EntityManager, entityCommandBuffer, zombieSpawner.ValueRO, localTransform.ValueRO, enemyPrefab);
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

    private static void SpawnEnemy(EntityManager entityManager, EntityCommandBuffer entityCommandBuffer, in ZombieSpawner zombieSpawner, in LocalTransform spawnerTransform, Entity enemyPrefab)
    {
        Entity zombieEntity = entityManager.Instantiate(enemyPrefab);
        entityManager.SetComponentData(zombieEntity, LocalTransform.FromPosition(zombieSpawner.zombieSpawnPosition));

        if (zombieSpawner.isRandomWalkingOnStart)
        {
            entityCommandBuffer.AddComponent(zombieEntity, new RandomWalking
            {
                originPosition = spawnerTransform.Position,
                targetPosition = spawnerTransform.Position,
                distanceMin = zombieSpawner.randomWalkingDistanceMin,
                distanceMax = zombieSpawner.randomWalkingDistanceMax,
                random = new Unity.Mathematics.Random((uint)zombieEntity.Index + 1u),
            });
        }
        else if (zombieSpawner.startTargetEntity != Entity.Null)
        {
            entityManager.SetComponentData(zombieEntity, new Target
            {
                targetEntity = zombieSpawner.startTargetEntity,
            });
        }
    }
}
