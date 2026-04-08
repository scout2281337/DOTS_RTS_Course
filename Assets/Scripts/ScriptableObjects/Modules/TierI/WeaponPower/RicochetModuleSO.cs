using UnityEngine;

[CreateAssetMenu(fileName = "RicochetModuleSO", menuName = "Scriptable Objects/Modules/TierI/RicochetModuleSO")]
public class RicochetModuleSO : ModuleBaseSO
{
    [Min(0f)] public float cooldown = 3f;
    [Min(0f)] public float ricochetRadius = 5f;
    [Min(0f)] public float damageMultiplier = 2f;
}
