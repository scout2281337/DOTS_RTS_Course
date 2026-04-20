using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXManager : Singleton<VFXManager>
{
    [SerializeField] private TrailVFXSO _trailsSO;
    [SerializeField] private BurstVFXSO _burstSO;
    [SerializeField] private SkillVFXSO _skillsSO;

    private IObjectPool<VFXObject> _bulletTrailVFXObjectPool;

    private IObjectPool<VFXObject> _explosionVFXObjectPool;
    private IObjectPool<VFXObject> _bloodBurstVFXObjectPool;
    private IObjectPool<VFXObject> _sparkBurstVFXObjectPool;

    private IObjectPool<VFXObject> _stimVFXObjectPool;
    private IObjectPool<VFXObject> _barricadeVFXObjectPool;
    private IObjectPool<VFXObject> _gaussVFXObjectPool;
    private IObjectPool<VFXObject> _scorcherVFXObjectPool;


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
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

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

    private VFXObject CreateVFXObject(IObjectPool<VFXObject> objectPool, Vector3 start, float duration = 1f)
    {
        VFXObject vfxObject = objectPool.Get();
        vfxObject.transform.position = start;

        vfxObject.duration = duration;
        vfxObject.PoolVFXObject(objectPool);

        return vfxObject;
    }

    private void CreateSoldierShot(BulletShotEvent evt)
    {
        if (evt.WeaponType != WeaponType.Dispersive)
            CreateVFXTrail(_bulletTrailVFXObjectPool, evt.Start, evt.End);

        if (evt.WeaponType == WeaponType.Explosive)
            CreateVFXObject(_explosionVFXObjectPool, evt.End);
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

    private async void CreateStimEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateStimEffect Activated" + evt.Type);

        // Spawn
        float duration = evt.Duration > 0 ? evt.Duration : 1f;
        var entities = DOTStoMono.GetSoldiersEntities();
        List<VFXObject> vfxObjects = new();
        foreach ( var entity in entities)
        {
            DOTStoMono.TryGetEntityPosition(entity, out var spawnPos);
            vfxObjects.Add(CreateVFXObject(_stimVFXObjectPool, spawnPos, duration));
        }

        // Update position
        var timer = Awaitable.WaitForSecondsAsync(duration);
        while (!timer.IsCompleted)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                DOTStoMono.TryGetEntityPosition(entities[i], out var currentPos);
                vfxObjects[i].transform.position = currentPos;
            }

            await Awaitable.NextFrameAsync();
        }
    }

    private void CreateBarricadeEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateBarricadeEffect Activated" + evt.Type);

        float duration = evt.Duration > 0 ? evt.Duration : 1f;
        CreateVFXObject(_barricadeVFXObjectPool, evt.End, duration);
    }

    private void CreateScorcherEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateScorcherEffect Activated" + evt.Type);

        float duration = evt.Duration > 0 ? evt.Duration : 1f;
        CreateVFXObject(_scorcherVFXObjectPool, evt.Start, duration);
    }

    private void CreateGaussEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateGaussEffect Activated" + evt.Type);

        CreateVFXTrail(_gaussVFXObjectPool, evt.Start, evt.End);
    }

    protected override void Awake()
    {
        base.Awake();

        _bulletTrailVFXObjectPool = NewObjectPool(_trailsSO.bulletTrailVFXObject);

        _explosionVFXObjectPool = NewObjectPool(_burstSO.explosionVFXObject);
        _bloodBurstVFXObjectPool = NewObjectPool(_burstSO.bloodBurstVFXObject);
        _sparkBurstVFXObjectPool = NewObjectPool(_burstSO.sparkBurstVFXObject);

        _stimVFXObjectPool = NewObjectPool(_skillsSO.StimVFXObject);
        _barricadeVFXObjectPool = NewObjectPool(_skillsSO.BarricadeVFXObject);
        _gaussVFXObjectPool = NewObjectPool(_skillsSO.GaussVFXObject);
        _scorcherVFXObjectPool = NewObjectPool(_skillsSO.ScorcherVFXObject);
    }

    private void Start()
    {
        EventMediator.Instance.OnBulletShot += CreateSoldierShot;
        EventMediator.Instance.OnAbilityStarted += CreateAbilityEffect;
    }
}
