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

    //List<Entity> SquadMembers = new List<Entity>();

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
        em.SetComponentData(currentEntity, new UnitMover
        {
            CurrentMoveSpeed = soldierConfig.bodyConfig.speed,
            BaseSpeed = soldierConfig.bodyConfig.speed,
            rotationSpeed = soldierConfig.bodyConfig.rotationSpeed,
        }) ;
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

        // После добавления абилки или просто при спавне решить, здесь думаю нормально , но это так мысли в комментарии я шиз лелелеле
        UnitClass unitClass = em.GetComponentData<Unit>(currentEntity).Class;
        unitEntityDict.Add(unitClass, currentEntity);

        AbilityEventListener.Instance.InvokeUnitSpawned(unitClass, soldierConfig);
    }

    Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + rnd.x, center.y, center.z + rnd.y);
    }

    private void OneClassSpawn() 
    {

        Debug.Log("Не воркает совсем");
        if (!isInitialized)
        {
            TryToInitialize();
            Debug.Log("Не воркает инициализация");
        }
        if (isInitialized && !isSpawned)
        {
            for (int i = 0; i < teamAmount; i++)
            {
                Vector3 spawnPos = RandomPointInCircle(Vector3.zero, 3f);
                UnitInitializer(entityManager, raiderConfig, spawnPos, entitiesReferences.unitPrefabEntity);

                Debug.Log("Спавн сработал");
                Debug.Log(entityManager.HasComponent<Prefab>(entitiesReferences.unitPrefabEntity));

            }
            isSpawned = true;
            //OnUnitSpawn?.Invoke();
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
            
            Vector3 spawnPos = RandomPointInCircle(Vector3.zero, 3f);
            
            UnitInitializer(entityManager, raiderConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, juggernautConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, arsonistConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, sniperConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            isSpawned = true;
        }    
    }
}
