using System;
using UnityEngine;
using UnityEngine.Pool;

public class VFXManager : Singleton<VFXManager>
{
    [SerializeField] private TrailVFXSO trailsSO;
    [SerializeField] private IObjectPool<VFXObject> bulletTrailVFXObjectPool;

    [SerializeField] private BurstVFXSO burstSO;
    [SerializeField] private IObjectPool<VFXObject> explosionVFXObjectPool;
    [SerializeField] private IObjectPool<VFXObject> bloodBurstVFXObjectPool;
    [SerializeField] private IObjectPool<VFXObject> sparkBurstVFXObjectPool;

    [SerializeField] private SkillVFXSO skillsSO;
    [SerializeField] private IObjectPool<VFXObject> anabolicVFXObjectPool;
    [SerializeField] private IObjectPool<VFXObject> gravityMagnifierVFXObjectPool;
    [SerializeField] private IObjectPool<VFXObject> railgunVFXObjectPool;
    [SerializeField] private IObjectPool<VFXObject> scorchingProjectileVFXObjectPool;


    #region Pooling
    private VFXObject OnNewPooledObject(VFXObject vfxObject)
    {
        VFXObject VFXInstance = Instantiate(vfxObject, this.transform);

        return VFXInstance;
    }

    private void OnReleaseToPool(VFXObject pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }

    private void OnGetFromPool(VFXObject pooledObject)
    {
        pooledObject.gameObject.SetActive(true);
    }

    private void OnDestroyPooledObject(VFXObject pooledObject)
    {
        DestroyImmediate(pooledObject);
    }

    private IObjectPool<VFXObject> NewObjectPool(VFXObject vfxObject)
    {
        return new ObjectPool<VFXObject>(
            () => OnNewPooledObject(vfxObject),
            OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject);
    }
    #endregion Pooling
    
    private void CreateVFXTrail(IObjectPool<VFXObject> objectPool, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        Vector3 midPoint = start + (direction * 0.5f);
        float length = direction.magnitude;
        Quaternion rotation = Quaternion.LookRotation(direction);

        VFXObject vfxObject = objectPool.Get();
        vfxObject.transform.SetPositionAndRotation(midPoint, rotation);
        vfxObject.transform.localScale = new Vector3(
            vfxObject.transform.localScale.x,
            vfxObject.transform.localScale.y,
            length
        );

        vfxObject.PoolVFXObject(objectPool);
    }

    private void CreateVFXObject(IObjectPool<VFXObject> objectPool, Vector3 start)
    {
        VFXObject vfxObject = objectPool.Get();
        vfxObject.transform.position = start;

        vfxObject.PoolVFXObject(objectPool);
    }

    private void CreateSoldierShot(BulletShotEvent evt)
    {
        if (evt.WeaponType != WeaponType.Dispersive)
            CreateVFXTrail(bulletTrailVFXObjectPool, evt.Start, evt.End);

        if (evt.WeaponType == WeaponType.Explosive)
            CreateVFXObject(explosionVFXObjectPool, evt.End);
    }

    private void CreateAbilityEffect(AbilityStartedEvent evt)
    {
        Action<AbilityStartedEvent> ability = evt.Type switch
        {
            AbilityType.Stim => CreateStimEffect,
            AbilityType.Barricade => CreateBarricadeEffect,
            AbilityType.Scorcher => CreateScorcherEffect,
            AbilityType.Gauss => CreateGaussEffect,

            _ => (evt) => { Debug.LogWarning("Event mismatch" + evt); }
        };

        ability?.Invoke(evt);
    }

    private void CreateStimEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateStimEffect Activated" + evt.Type);
        // Нужно как-то получить расположение всех солдат
    }

    private void CreateBarricadeEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateBarricadeEffect Activated" + evt.Type);
        // Расположение точки в которой находится баррикада и солдат
    }

    private void CreateScorcherEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateScorcherEffect Activated" + evt.Type);
        // Расположение точки в которой находится огниво и солдат
    }

    private void CreateGaussEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateGaussEffect Activated" + evt.Type);
        // Расположение точки в которую направлен гаусс и точка из которой выходят пули
    }

    protected override void Awake()
    {
        base.Awake();

        bulletTrailVFXObjectPool = NewObjectPool(trailsSO.bulletTrailVFXObject);

        explosionVFXObjectPool = NewObjectPool(burstSO.explosionVFXObject);
        bloodBurstVFXObjectPool = NewObjectPool(burstSO.bloodBurstVFXObject);
        sparkBurstVFXObjectPool = NewObjectPool(burstSO.sparkBurstVFXObject);

        anabolicVFXObjectPool = NewObjectPool(skillsSO.anabolicVFXObject);
        gravityMagnifierVFXObjectPool = NewObjectPool(skillsSO.gravityMagnifierVFXObject);
        railgunVFXObjectPool = NewObjectPool(skillsSO.railgunVFXObject);
        scorchingProjectileVFXObjectPool = NewObjectPool(skillsSO.scorchingProjectileVFXObject);
    }

    private void Start()
    {
        EventMediator.Instance.OnBulletShot += CreateSoldierShot;
        EventMediator.Instance.OnAbilityStarted += CreateAbilityEffect;
    }
}