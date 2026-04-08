using UnityEngine;

[CreateAssetMenu(fileName = "AcidBulletsModuleSO", menuName = "Scriptable Objects/Modules/TierI/AcidBulletsModuleSO")]
public class AcidBulletsModuleSO : ModuleBaseSO
{
    [Min(0f)] public float moveSpeedReduction = 0.5f;
    [Min(0f)] public float vulnerabilityIncrease = 0.5f;
    [Min(1)] public int maxStacks = 60;
}
