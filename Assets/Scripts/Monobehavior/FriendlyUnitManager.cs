using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FriendlyUnitManager : MonoBehaviour
{
    public WeaponConfig weaponConfig;
    public ClassConfig classConfig;
    public int teamAmount;
    
    private EntitiesReferences entitiesReferences;
    private EntityManager entityManager;

    private bool isInitialized = false;
    private bool isSpawned = false;

    //public Entity[] Units; мб убрать
    public Dictionary<UnitClass, Entity> EntitiesDictionary = new Dictionary<UnitClass, Entity>();
    //public static event Action OnUnitSpawn;
    public static FriendlyUnitManager Instance { get; private set; }
    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Update()
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
                UnitInitializer(entityManager, weaponConfig, classConfig, spawnPos, entitiesReferences.unitPrefabEntity);

                Debug.Log("—павн сработал");
                Debug.Log(entityManager.HasComponent<Prefab>(entitiesReferences.unitPrefabEntity));

            }
            isSpawned = true;
            //OnUnitSpawn?.Invoke();
        }
    }
    void TryToInitialize() 
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

    public void UnitInitializer(EntityManager em, WeaponConfig weaponConfig, ClassConfig classConfig, float3 startPos, Entity entityToSpawn) 
    {
        Entity currentEntity = em.Instantiate(entityToSpawn);
        em.SetComponentData(currentEntity, LocalTransform.FromPosition(startPos));
        em.SetComponentData(currentEntity, new UnitMover
        {
            CurrentMoveSpeed = classConfig.speed,
            BaseSpeed = classConfig.speed,
            rotationSpeed = classConfig.rotationSpeed,
        }) ;
        em.SetComponentData(currentEntity, new FindTarget
        {
            range = weaponConfig.range, 
            targetFaction = classConfig.targetFaction,
            timer = 0,
            timerMax = classConfig.timerMaxForOverlap,
        });
        em.SetComponentData(currentEntity, new ShootAttack
        {
            timerMax = 60 / weaponConfig.fireRate,
            timer = 60f / weaponConfig.fireRate,
            damageAmount = weaponConfig.damage,
            attackDistance = weaponConfig.range,
        });
        em.SetComponentData(currentEntity, new Health
        {
            healthAmountMax = classConfig.maxHealth,
            healthAmount = classConfig.maxHealth,
            Armour = classConfig.Armour,
        });
        
        
        // ѕосле добавлени€ абилки или просто при спавне решить, здесь думаю нормально , но это так мысли в комментарии € шиз лелелеле
        //EntitiesDictionary.Add(em.GetComponentData<Unit>(currentEntity).Class, currentEntity);
    }

    Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + rnd.x, center.y, center.z + rnd.y);
    }
}
