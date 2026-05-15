using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RespawnManager : Singleton<RespawnManager>
{
     
    
    private void Update()
    {
        
    }
    public bool RequestRespawn(UnitClass unitClass, float3? customPosition = null)
    {
        if (FriendlyUnitManager.Instance == null)
            return false;

        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out Entity entity))
            return false;

        return RequestRespawn(entity, customPosition);
    }

    public bool RequestRespawn(Entity entity, float3? customPosition = null)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!em.Exists(entity) || !em.HasComponent<DeadUnit>(entity))
            return false;

        if (em.HasComponent<RespawnRequest>(entity))
            return true;

        em.AddComponentData(entity, new RespawnRequest
        {
            Position = customPosition ?? float3.zero,
            UseCustomPosition = customPosition.HasValue
        });

        return true;
    }


}
