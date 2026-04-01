using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class VFXObject : MonoBehaviour
{
    public float duration = 1;
    public VisualEffect vfx;

    public IEnumerator PoolVFXObject(IObjectPool<VFXObject> objectPool)
    {
        yield return new WaitForSeconds(duration);

        objectPool.Release(this);
    }
}
