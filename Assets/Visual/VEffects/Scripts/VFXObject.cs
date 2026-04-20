using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class VFXObject : MonoBehaviour
{
    public float duration = 1f;
    public VisualEffect vfx;

    public async void PoolVFXObject(IObjectPool<VFXObject> objectPool)
    {
        await Awaitable.WaitForSecondsAsync(1f);

        objectPool.Release(this);
    }
}