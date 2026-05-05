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
    private IObjectPool<VFXObject> _rangePool;
    private IObjectPool<VFXObject> _linePool;
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

    #region Create VFX
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
        VFXObject vfxObject = CreateVFXObjectDirected(objectPool, start, end, duration);

        float length = (end - start).magnitude;
        vfxObject.transform.localScale = new Vector3(
            vfxObject.transform.localScale.x,
            vfxObject.transform.localScale.y,
            length
        );

        return vfxObject;
    }
    #endregion CreateVFX

    #region Weapon
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
    #endregion Weapon

    #region Ability
    #region Pointing
    private interface IAbilityPointer
    {
        void Initialize(AbilityPointerEvent evt);
        void Update(Vector3 mousePos, Vector3 casterPos);
        void Dispose();
    }

    private class PointPointer : IAbilityPointer
    {
        private readonly IObjectPool<VFXObject> _pool;
        private VFXObject _vfx;
        private float _range;

        public PointPointer(IObjectPool<VFXObject> pool)
        {
            _pool = pool;
        }

        public void Initialize(AbilityPointerEvent evt)
        {
            _vfx = _pool.Get();
            _range = evt.Range;

            float scale = evt.Area <= 0 ? 0.5f : evt.Area * 2;
            _vfx.transform.localScale = new Vector3(scale, 1, scale);
        }

        public void Update(Vector3 mousePos, Vector3 casterPos)
        {
            Vector3 dir = mousePos - casterPos;

            Vector3 clamped = dir.normalized * _range + casterPos;
            Vector3 finalPos = dir.magnitude <= _range ? mousePos : clamped;

            _vfx.transform.position = finalPos;
        }

        public void Dispose()
        {
            _vfx?.PoolVFXObject(_pool);
        }
    }

    private class LinePointer : IAbilityPointer
    {
        private readonly IObjectPool<VFXObject> _pool;
        private VFXObject _vfx;

        public LinePointer(IObjectPool<VFXObject> pool)
        {
            _pool = pool;
        }

        public void Initialize(AbilityPointerEvent evt)
        {
            _vfx = _pool.Get();

            float width = evt.Area <= 0 ? 0.5f : evt.Area * 2;
            float range = evt.Range <= 0 ? 1 : evt.Range;

            _vfx.transform.localScale = new Vector3(width, 1, range);
        }

        public void Update(Vector3 mousePos, Vector3 casterPos)
        {
            Vector3 dir = mousePos - casterPos;
            Quaternion rot = Quaternion.LookRotation(dir);

            _vfx.transform.SetPositionAndRotation(casterPos, rot);
        }

        public void Dispose()
        {
            _vfx?.PoolVFXObject(_pool);
        }
    }

    private async void StartPointingAbility(AbilityPointerEvent evt)
    {
        if (_isPointing) return;
        _isPointing = true;

        IAbilityPointer pointer = evt.PointerType switch
        {
            AbilityPointerType.PointFromCaster => new PointPointer(_pointerPool),
            AbilityPointerType.LineFromCaster => new LinePointer(_linePool),
            _ => null
        };

        pointer?.Initialize(evt);

        var rangeVFX = _rangePool.Get();
        float rangeScale = evt.Range <= 0 ? 1 : evt.Range * 2;
        rangeVFX.transform.localScale = new Vector3(rangeScale, 1, rangeScale);

        while (_isPointing)
        {
            Vector3 mousePos = Utilities.GetMouseWorldPosition();
            DOTStoMono.TryGetEntityPosition(evt.Caster, out var casterPos);

            pointer?.Update(mousePos, casterPos);
            rangeVFX.transform.position = casterPos;

            await Awaitable.NextFrameAsync();
        }

        pointer?.Dispose();
        rangeVFX.PoolVFXObject(_rangePool);
    }

    private void EndPointingAbility()
    {
        _isPointing = false;
    }
    #endregion Pointing

    #region Ability
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
        CreateVFXObjectAtPoint(_scorcherPool, evt.End, duration);
    }

    private void CreateGaussEffect(AbilityStartedEvent evt)
    {
        Debug.Log("CreateGaussEffect Activated" + evt.Type);

        CreateVFXTrail(_gaussPool, evt.Start, evt.End);
    }
    #endregion Ability
    #endregion Ability


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
        _rangePool = NewObjectPool(_skillsSO.RangeVFXObject);
        _linePool = NewObjectPool(_skillsSO.LineVFXObject);
        _stimPool = NewObjectPool(_skillsSO.StimVFXObject);
        _barricadePool = NewObjectPool(_skillsSO.BarricadeVFXObject);
        _gaussPool = NewObjectPool(_skillsSO.GaussVFXObject);
        _scorcherPool = NewObjectPool(_skillsSO.ScorcherVFXObject);
    }

    private void Start()
    {
        EventMediator.Instance.OnBulletShot += CreateSoldierShot;
        EventMediator.Instance.OnDamageReceived += CreateDamageEffect;
        EventMediator.Instance.OnAbilityStarted += HandleAbilityStarted;
        EventMediator.Instance.OnAbilityPointer += StartPointingAbility;
        EventMediator.Instance.OnAbilityPointerEnded += HandleAbilityPointerEnded;
    }

    private void HandleAbilityStarted(AbilityStartedEvent evt)
    {
        EndPointingAbility();
        CreateAbilityEffect(evt);
    }

    private void HandleAbilityPointerEnded(AbilityPointerEndedEvent evt)
    {
        EndPointingAbility();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            var caster = DOTStoMono.GetSoldiersEntities()[0];
            AbilityPointerEvent evt = new()
            {
                PointerType = AbilityPointerType.LineFromCaster,
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
