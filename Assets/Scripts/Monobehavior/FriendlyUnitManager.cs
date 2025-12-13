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

    public Entity[] Units;
    private void Awake()
    {
        Units = new Entity[teamAmount];
    }
    private void Start()
    {
            
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
                UnitInitializer(entityManager, weaponConfig, classConfig, spawnPos, entitiesReferences.unitPrefabEntity, i);

                Debug.Log("Спавн сработал");
                Debug.Log(entityManager.HasComponent<Prefab>(entitiesReferences.unitPrefabEntity));

            }
            isSpawned = true;
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

    public void UnitInitializer(EntityManager em, WeaponConfig weaponConfig, ClassConfig classConfig, float3 startPos, Entity entityToSpawn, int id) 
    {
        Entity currentEntity = em.Instantiate(entityToSpawn);
        Units[id] = currentEntity;
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
            timerMax = 1 / weaponConfig.fireRate,
            damageAmount = weaponConfig.damage,
            attackDistance = weaponConfig.range,
        });
        em.SetComponentData(currentEntity, new Health
        {
            healthAmountMax = classConfig.maxHealth,
            healthAmount = classConfig.maxHealth,
        });
    }

    Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + rnd.x, center.y, center.z + rnd.y);
    }
}
