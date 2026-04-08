using UnityEngine;

[CreateAssetMenu(fileName = "EnergyVampireModuleSO", menuName = "Scriptable Objects/Modules/TierI/EnergyVampireModuleSO")]
public class EnergyVampireModuleSO : ModuleBaseSO
{
    [Min(0f)] public float cooldownReduction = 0.1f;
}
