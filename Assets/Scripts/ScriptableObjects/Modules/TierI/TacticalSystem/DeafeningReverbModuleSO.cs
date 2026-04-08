using UnityEngine;

[CreateAssetMenu(fileName = "DeafeningReverbModuleSO", menuName = "Scriptable Objects/Modules/TierI/DeafeningReverbModuleSO")]
public class DeafeningReverbModuleSO : ModuleBaseSO
{
    [Min(0f)] public float stunDuration = 0.3f;
}
