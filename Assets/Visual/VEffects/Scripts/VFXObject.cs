using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class VFXObject : MonoBehaviour
{
    public float duration = 1f;
    public VisualEffect vfx;

    private Vector3 initialLocalScale;

    private void Awake()
    {
        initialLocalScale = transform.localScale;
    }

    public void ResetLocalScale()
    {
        transform.localScale = initialLocalScale;
    }

    public async void PoolVFXObject(IObjectPool<VFXObject> objectPool)
    {
        await Awaitable.WaitForSecondsAsync(duration);

        objectPool.Release(this);
    }
}
