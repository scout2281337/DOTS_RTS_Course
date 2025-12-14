using UnityEngine;
using UnityEngine.Pool;

public class VFXManager : Singleton<VFXManager>
{
    public TrailVFXSO trailsSO;
    public IObjectPool<VFXObject> bulletTrailVFXObjectPool;

    public BurstVFXSO burstSO;
    public IObjectPool<VFXObject> explosionVFXObjectPool;
    public IObjectPool<VFXObject> bloodBurstVFXObjectPool;
    public IObjectPool<VFXObject> sparkBurstVFXObjectPool;

    public SkillVFXSO skillsSO;
    public IObjectPool<VFXObject> anabolicVFXObjectPool;
    public IObjectPool<VFXObject> gravityMagnifierVFXObjectPool;
    public IObjectPool<VFXObject> railgunVFXObjectPool;
    public IObjectPool<VFXObject> scorchingProjectileVFXObjectPool;


    public void CreateTrail(IObjectPool<VFXObject> objectPool, Vector3 start, Vector3 end)
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

        StartCoroutine(vfxObject.PoolVFXObject(objectPool));
    }

    private IObjectPool<VFXObject> NewObjectPool(VFXObject vfxObject)
    {
        return new ObjectPool<VFXObject>(
            () => OnNewPooledObject(vfxObject),
            OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject);
    }

    private VFXObject OnNewPooledObject(VFXObject vfxObject)
    {
        VFXObject VFXInstance = Instantiate(vfxObject);

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


    protected override void Awake()
    {
        base.Awake();

        bulletTrailVFXObjectPool = NewObjectPool(trailsSO.bulletTrailVFXObject);

        explosionVFXObjectPool = NewObjectPool(burstSO.explosionVFXObject);
        bloodBurstVFXObjectPool = NewObjectPool(burstSO.bloodBurstVFXObject);
        sparkBurstVFXObjectPool = NewObjectPool(burstSO.bloodBurstVFXObject);

        anabolicVFXObjectPool = NewObjectPool(skillsSO.anabolicVFXObject);
        gravityMagnifierVFXObjectPool = NewObjectPool(skillsSO.gravityMagnifierVFXObject);
        railgunVFXObjectPool = NewObjectPool(skillsSO.railgunVFXObject);
        scorchingProjectileVFXObjectPool = NewObjectPool(skillsSO.scorchingProjectileVFXObject);
    }
}
