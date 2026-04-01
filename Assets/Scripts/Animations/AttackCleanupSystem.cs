using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AttackCleanupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        //var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        ////foreach (var (attackRequest, entity) in SystemAPI.Query<AttackRequest>().WithEntityAccess())
        ////{
        ////    // удаляем запрос после одного кадра
        ////    ecb.RemoveComponent<AttackRequest>(entity);
        ////}

        //ecb.Playback(state.EntityManager);
        //ecb.Dispose();
    }
}