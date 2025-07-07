using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct TestingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
        int UnitCount = 0;

        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<UnitMover> unitMover,
            RefRW<PhysicsVelocity> physicsVelocity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<UnitMover>,
                RefRW<PhysicsVelocity>>().WithPresent<Selected>())
        {
            UnitCount++;    

        }
        Debug.Log("Unit count" + UnitCount);
        */
    }

}
