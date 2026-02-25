using Unity.Entities;
using UnityEngine;

public class CubeSpawnerAuthoring : MonoBehaviour
{
    public int amountToSpawn;
    public GameObject cubePrefabGameObject;
    public float timerMax;
    public class Baker : Baker<CubeSpawnerAuthoring>
    {
        public override void Bake(CubeSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CubeSpawnerComponent
            {
                amountToSpawn = authoring.amountToSpawn,
                cubePrefabEntity = GetEntity(authoring.cubePrefabGameObject, TransformUsageFlags.Dynamic),
                timerMax = authoring.timerMax,
                random = new Unity.Mathematics.Random(1),//(uint)Random.Radius(1, 100)
                timer = 0f,
            }); 
        }
    }
}
public struct CubeSpawnerComponent : IComponentData 
{
    public int amountToSpawn;
    public Entity cubePrefabEntity;
    public float timerMax;
    public Unity.Mathematics.Random random;
    public float timer;
}
