using UnityEngine;

[CreateAssetMenu(fileName = "BerserkModuleSO", menuName = "Scriptable Objects/Modules/TierI/BerserkModuleSO")]
public class BerserkModuleSO : ModuleBaseSO
{
    [Min(0f)] public float triggerHealth = 50;
    [Min(0f)] public float maxDamageBonus = 50;
}
