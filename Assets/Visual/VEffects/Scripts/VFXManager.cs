using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXManager : Singleton<VFXManager>
{
    [SerializeField] private TrailVFXSO _trailsSO;
    [SerializeField] private WeaponVFXSO _weaponSO;
    [SerializeField] private SkillVFXSO _skillsSO;

    private IObjectPool<VFXObject> _bulletTrailPool;

    private IObjectPool<VFXObject> _muzzleFlashPool;
    private IObjectPool<VFXObject> _explosionPool;
    private IObjectPool<VFXObject> _bloodBurstPool;
    private IObjectPool<VFXObject> _bloodSplashBurstPool;
    private IObjectPool<VFXObject> _sparkBurstPool;

    private IObjectPool<VFXObject> _pointerPool;
    private IObjectPool<VFXObject> _pointerAreaPool;
    private IObjectPool<VFXObject> _stimPool;
    private IObjectPool<VFXObject> _barricadePool;
    private IObjectPool<VFXObject> _gaussPool;
    private IObjectPool<VFXObject> _scorcherPool;

    private bool _isPointing = false;


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

    private VFXObject CreateVFXObject(IObjectPool<VFXObject> objectPool, float duration = 1f)
    {
        VFXObject vfxObject = objectPool.Get();

        vfxObject.duration = duration;
        vfxObject.PoolVFXObject(objectPool);

        return vfxObject;
    }

    private VFXObject CreateVFXObjectAtPoint(IObjectPool<VFXObject> objectPool, Vector3 start, float duration = 1f)
    {
        VFXObject vfxObject = CreateVFXObject(objectPool, duration);
        vfxObject.transform.position = start;

        return vfxObject;
    }

    private VFXObject CreateVFXObjectDirected(IObjectPool<VFXObject> objectPool, Vector3 start, Vector3 end, float duration = 1f)
    {
        VFXObject vfxObject = CreateVFXObject(objectPool, duration);

        Vector3 direction = end - start;
        Quaternion rotation = Quaternion.LookRotation(direction);

        vfxObject.transform.SetPositionAndRotation(start, rotation);

        return vfxObject;
    }

    private VFXObject CreateVFXTrail(IObjectPool<VFXObject> objectPool, Vector3 start, Vector3 end, float duration = 1f)
    {
        Vector3 direction = end - start;
        Vector3 midPoint = start + (direction * 0.5f);
        float length = direction.magnitude;

        VFXObject vfxObject = CreateVFXObjectDirected(objectPool, midPoint, end, duration);
        vfxObject.transform.localScale = new Vector3(
            vfxObject.transform.localScale.x,
            vfxObject.transform.localScale.y,
            length
        );

        return vfxObject;
    }

    private void CreateSoldierShot(BulletShotEvent evt)
    {
        if (evt.WeaponType != WeaponType.Dispersive)
        {
            CreateVFXTrail(_bulletTrailPool, evt.Start, evt.End);
            CreateVFXObjectDirected(_muzzleFlashPool, evt.Start, evt.End);
        }

        if (evt.WeaponType == WeaponType.Explosive)
            CreateVFXObjectAtPoint(_explosionPool, evt.End);
    }

    private void CreateDamageEffect(DamageEvent evt)
    {
        if (evt.IsAbilityDamage) return;
        if (!DOTStoMono.TryGetEntityPosition(evt.TargetEntity, out Vector3 position)) return;

        if (evt.TargetEntityClass == UnitClass.Robot)
        {
            CreateVFXObjectAtPoint(_bloodSplashBurstPool, position, 10f);
            CreateVFXObjectAtPoint(_sparkBurstPool, position + Vector3.up);
        }
        else
        {
            CreateVFXObjectAtPoint(_bloodSplashBurstPool, position, 10f);
        }
    }

    private async void StartPointingAbility(AbilityPointerEvent evt)
    {
        _isPointing = true;
        VFXObject vfxObjectPointer = _pointerPool.Get();
        VFXObject vfxObjectArea = _pointerAreaPool.Get();

        while (_isPointing)
        {
            vfxObjectPointer.transform.position = Utilities.GetMouseWorldPosition();
            float pointerScale = evt.Area <= 0 ? 0.5f : evt.Area;
            vfxObjectPointer.transform.localScale = new Vector3(pointerScale, 1, pointerScale);

            DOTStoMono.TryGetEntityPosition(evt.Caster, out var casterPosition);
            vfxObjectArea.transform.position = casterPosition;
            float areaScale = evt.Range <= 0 ? 3 : evt.Range;
            vfxObjectArea.transform.localScale = new Vector3(areaScale, 1, areaScale);

            await Awaitable.NextFrameAsync();
        }

        vfxObjectPointer.duration = 0;
        vfxObjectArea.duration = 0;
        vfxObjectPointer.PoolVFXObject(_pointerPool);
        vfxObjectArea.PoolVFXObject(_pointerAreaPool);
    }

    private void EndPointingAbility()
    {
        _isPointing = false;
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
            vfxObjects.Add(CreateVFXObjectAtPoint(_stimPool, spawnPos, duration));
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
        CreateVFXObjectAtPoint(_barricadePool, evt.End, duration);
    }

    private void CreateScorcherEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateScorcherEffect Activated" + evt.Type);

        float duration = evt.Duration > 0 ? evt.Duration : 1f;
        CreateVFXObjectAtPoint(_scorcherPool, evt.Start, duration);
    }

    private void CreateGaussEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateGaussEffect Activated" + evt.Type);

        CreateVFXTrail(_gaussPool, evt.Start, evt.End);
    }


    protected override void Awake()
    {
        base.Awake();

        _bulletTrailPool = NewObjectPool(_trailsSO.bulletTrailVFXObject);

        _muzzleFlashPool = NewObjectPool(_weaponSO.MuzzleFlashVFXObject);
        _explosionPool = NewObjectPool(_weaponSO.ExplosionVFXObject);
        _bloodBurstPool = NewObjectPool(_weaponSO.BloodBurstVFXObject);

        _bloodSplashBurstPool = NewObjectPool(_weaponSO.BloodSplashBurstVFXObject);
        _sparkBurstPool = NewObjectPool(_weaponSO.SparkBurstVFXObject);

        _pointerPool = NewObjectPool(_skillsSO.PointerVFXObject);
        _pointerAreaPool = NewObjectPool(_skillsSO.AreaVFXObject);
        _stimPool = NewObjectPool(_skillsSO.StimVFXObject);
        _barricadePool = NewObjectPool(_skillsSO.BarricadeVFXObject);
        _gaussPool = NewObjectPool(_skillsSO.GaussVFXObject);
        _scorcherPool = NewObjectPool(_skillsSO.ScorcherVFXObject);
    }

    private void Start()
    {
        EventMediator.Instance.OnBulletShot += CreateSoldierShot;
        EventMediator.Instance.OnDamageReceived += CreateDamageEffect;
        EventMediator.Instance.OnAbilityStarted += CreateAbilityEffect;
        EventMediator.Instance.OnAbilityPointer += StartPointingAbility;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            var caster = DOTStoMono.GetSoldiersEntities()[0];
            AbilityPointerEvent evt = new()
            {
                Caster = caster,
                Range = 10,
                Area = 2
            };
            EventMediator.Instance.InvokeAbilityPointer(evt);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            EndPointingAbility();
        }
    }
}
