using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FriendlyUnitManager : Singleton<FriendlyUnitManager>
{
    [Header("Огнеметчик")]
    public WeaponConfig arsonistWeaponConfig;
    public BodyConfig arsonistBodyConfig;
    [Header("Джаггернаут")]
    public WeaponConfig juggernautWeaponConfig;
    public BodyConfig juggernautBodyConfig;
    [Header("Рейдер")]
    public WeaponConfig raiderWeaponConfig;
    public BodyConfig raiderBodyConfig;
    [Header("Снайпер")]
    public WeaponConfig sniperWeaponConfig;
    public BodyConfig sniperBodyConfig;

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
        //TeamSpawn();
        OneClassSpawn();
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

    public void UnitInitializer(EntityManager em, WeaponConfig weaponConfig, BodyConfig bodyConfig, float3 startPos, Entity entityToSpawn) 
    {
        Entity currentEntity = em.Instantiate(entityToSpawn);
        em.SetComponentData(currentEntity, LocalTransform.FromPosition(startPos));
        em.SetComponentData(currentEntity, new UnitMover
        {
            CurrentMoveSpeed = bodyConfig.speed,
            BaseSpeed = bodyConfig.speed,
            rotationSpeed = bodyConfig.rotationSpeed,
        }) ;
        em.SetComponentData(currentEntity, new FindTarget
        {
            range = weaponConfig.range, 
            targetFaction = bodyConfig.targetFaction,
            timer = 0,
            timerMax = bodyConfig.timerMaxForOverlap,
        });
        em.SetComponentData(currentEntity, new ShootAttack
        {
            timerMax = CooldownFromFireRate(weaponConfig.fireRate),
            timer = CooldownFromFireRate(weaponConfig.fireRate),
            damageAmount = weaponConfig.damage,
            attackDistance = weaponConfig.range,
            weaponType = weaponConfig.weaponType,
            maxPierceCount = weaponConfig.maxPierceCount,
            explosiveRange = weaponConfig.explosiveRange,
            attackMode = AttackMode.Normal,
            ChargedAttackDamage = weaponConfig.ChargedAttackDamage
        });
        em.SetComponentData(currentEntity, new Health
        {
            healthAmountMax = bodyConfig.maxHealth,
            healthAmount = bodyConfig.maxHealth,
            armor = bodyConfig.armor,
        });
        em.SetComponentData(currentEntity, new Unit
        {
            Class = bodyConfig.unitClass,
            faction = bodyConfig.currentFaction,
        });
        // После добавления абилки или просто при спавне решить, здесь думаю нормально , но это так мысли в комментарии я шиз лелелеле
        unitEntityDict.Add(em.GetComponentData<Unit>(currentEntity).Class, currentEntity);
    }

    Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + rnd.x, center.y, center.z + rnd.y);
    }

    private void OneClassSpawn() 
    {
        if (!isInitialized)
        {
            TryToInitialize();
        }
        if (isInitialized && !isSpawned)
        {
            for (int i = 0; i < teamAmount; i++)
            {
                Vector3 spawnPos = RandomPointInCircle(Vector3.zero, 3f);
                UnitInitializer(entityManager, raiderWeaponConfig, raiderBodyConfig, spawnPos, entitiesReferences.unitPrefabEntity);

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
            
            UnitInitializer(entityManager, raiderWeaponConfig, raiderBodyConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, juggernautWeaponConfig, juggernautBodyConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, arsonistWeaponConfig, arsonistBodyConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            UnitInitializer(entityManager, sniperWeaponConfig, sniperBodyConfig, RandomPointInCircle(Vector3.zero, 3f), entitiesReferences.unitPrefabEntity);
            isSpawned = true;
        }    
    }
}
