using Unity.Entities;

public struct SlowDebuff : IBufferElementData
{
    public float Multiplier; 
    public Entity Source;    // кто дал дебафф
    public float Timer;
}
