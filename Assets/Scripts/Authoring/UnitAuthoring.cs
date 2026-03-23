using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class UnitAuthoring : MonoBehaviour
{
    public Faction faction;
    public UnitClass Class;
    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Unit 
            {
                faction = authoring.faction,
                Class = authoring.Class,
            });
            //AddComponent<NavPathProgress>(entity);
            //AddBuffer<NavPathPoint>(entity);
        }
    }
}

public struct Unit : IComponentData 
{
    public Faction faction;
    public UnitClass Class;
}

public enum UnitClass 
{
    Raider,
    Arsonist,
    Juggernaut,
    Sniper,
    Robot
}