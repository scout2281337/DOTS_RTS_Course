using UnityEngine;

[CreateAssetMenu(fileName = "CenterOfAttentionModuleSO", menuName = "Scriptable Objects/Modules/TierI/CenterOfAttentionModuleSO")]
public class CenterOfAttentionModuleSO : ModuleBaseSO
{
    [Min(0f)] public float fireRateBonus = 0.3f;
    [Min(1)] public int maxStacks = 60;
}
