using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FriendlyUnitManager : Singleton<FriendlyUnitManager>
{
    public SoldierAttributesConfig arsonistConfig;
    public SoldierAttributesConfig juggernautConfig;
    public SoldierAttributesConfig raiderConfig;
    public SoldierAttributesConfig sniperConfig;

    public int teamAmount;

    private EntitiesReferences entitiesReferences;
    private EntityManager entityManager;

    private bool isInitialized = false;
    private bool isSpawned = false;

    public Dictionary<UnitClass, Entity> unitEntityDict = new();

    private static float CooldownFromFireRate(float fireRate)
    {
        return fireRate > 0f ? 60f / fireRate : float.MaxValue;
    }

    private void Update()
    {
        TeamSpawn();
        //OneClassSpawn();
    }

    private void TryToInitialize()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            return;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var query = entityManager.CreateEntityQuery(typeof(EntitiesReferences));
        if (query.CalculateEntityCount() == 0)
            return;

        entitiesReferences = query.GetSingleton<EntitiesReferences>();
        Debug.Log("EntitiesReferences loaded!");
        isInitialized = true;
    }

    public void UnitInitializer(EntityManager em, SoldierAttributesConfig soldierConfig, float3 startPos, Entity entityToSpawn)
    {
        Entity currentEntity = em.Instantiate(entityToSpawn);
        em.SetComponentData(currentEntity, LocalTransform.FromPosition(startPos));

        UnitMover unitMover = em.HasComponent<UnitMover>(currentEntity)
            ? em.GetComponentData<UnitMover>(currentEntity)
            : default;

        unitMover.CurrentMoveSpeed = soldierConfig.bodyConfig.speed;
        unitMover.BaseSpeed = soldierConfig.bodyConfig.speed;
        unitMover.rotationSpeed = soldierConfig.bodyConfig.rotationSpeed;
        unitMover.targetPosition = startPos;

        em.SetComponentData(currentEntity, unitMover);
        em.SetComponentData(currentEntity, new FindTarget
        {
            range = soldierConfig.weaponConfigs[0].range,
            targetFaction = soldierConfig.bodyConfig.targetFaction,
            timer = 0,
            timerMax = soldierConfig.bodyConfig.timerMaxForOverlap,
        });
        em.SetComponentData(currentEntity, new ShootAttack
        {
            timerMax = CooldownFromFireRate(soldierConfig.weaponConfigs[0].fireRate),
            timer = CooldownFromFireRate(soldierConfig.weaponConfigs[0].fireRate),
            damageAmount = soldierConfig.weaponConfigs[0].damage,
            attackDistance = soldierConfig.weaponConfigs[0].range,
            weaponType = soldierConfig.weaponConfigs[0].weaponType,
            maxPierceCount = soldierConfig.weaponConfigs[0].maxPierceCount,
            explosiveRange = soldierConfig.weaponConfigs[0].explosiveRange,
            attackMode = AttackMode.Normal,
            ChargedAttackDamage = soldierConfig.weaponConfigs[0].ChargedAttackDamage
        });
        em.SetComponentData(currentEntity, new Health
        {
            healthAmountMax = soldierConfig.bodyConfig.maxHealth,
            healthAmount = soldierConfig.bodyConfig.maxHealth,
            armor = soldierConfig.bodyConfig.armor,
        });
        em.SetComponentData(currentEntity, new Unit
        {
            Class = soldierConfig.bodyConfig.unitClass,
            faction = soldierConfig.bodyConfig.currentFaction,
        });

        Ability abilityData = default;
        if (em.HasComponent<Ability>(currentEntity))
        {
            abilityData = em.GetComponentData<Ability>(currentEntity);
        }

        if (soldierConfig.skillConfigs != null && soldierConfig.skillConfigs.Length > 0 && soldierConfig.skillConfigs[0] != null)
        {
            var skillConfig = soldierConfig.skillConfigs[0];
            abilityData.Type = skillConfig.type;
            abilityData.TargetType = skillConfig.targetType;
            abilityData.Cooldown = skillConfig.cooldown;
            abilityData.Duration = skillConfig.duration;
        }
        else
        {
            abilityData.Type = AbilityType.None;
            abilityData.TargetType = AbilityTargetType.Self;
            abilityData.Cooldown = 0f;
            abilityData.Duration = 0f;
        }

        abilityData.Owner = currentEntity;
        abilityData.Active = false;
        abilityData.IsTriggered = false;
        abilityData.TimeLeft = 0f;
        abilityData.CooldownLeft = 0f;
        abilityData.TargetPosition = default;
        //abilityData.TargetType = soldierConfig.skillConfigs[0].targetType;

        if (em.HasComponent<Ability>(currentEntity))
        {
            em.SetComponentData(currentEntity, abilityData);
        }
        else
        {
            em.AddComponentData(currentEntity, abilityData);
        }

        UnitClass unitClass = em.GetComponentData<Unit>(currentEntity).Class;
        unitEntityDict.Add(unitClass, currentEntity);

        EventMediator.Instance.InvokeUnitSpawned(unitClass, soldierConfig);
    }

    Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + rnd.x, center.y, center.z + rnd.y);
    }

    private void OneClassSpawn()
    {
        Debug.Log("OneClassSpawn is running");
        if (!isInitialized)
        {
            TryToInitialize();
            Debug.Log("Initialization was requested");
        }

        if (isInitialized && !isSpawned)
        {
            for (int i = 0; i < teamAmount; i++)
            {
                Vector3 spawnPos = RandomPointInCircle(Vector3.zero, 3f);
                UnitInitializer(entityManager, raiderConfig, spawnPos, entitiesReferences.unitPrefabEntity);

                Debug.Log("Spawn completed");
                Debug.Log(entityManager.HasComponent<Prefab>(entitiesReferences.unitPrefabEntity));
            }

            isSpawned = true;
        }
    }

    private void TeamSpawn()
    {
        if (!isInitialized)
        {
            TryToInitialize();
        }

        if (isInitialized && !isSpawned)
        {
            UnitInitializer(entityManager, raiderConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, juggernautConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, arsonistConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, sniperConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            isSpawned = true;
        }
    }
}
