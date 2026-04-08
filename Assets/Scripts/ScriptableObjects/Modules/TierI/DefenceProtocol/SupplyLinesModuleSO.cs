using UnityEngine;

[CreateAssetMenu(fileName = "SupplyLinesModuleSO", menuName = "Scriptable Objects/Modules/TierI/SupplyLinesModuleSO")]
public class SupplyLinesModuleSO : ModuleBaseSO
{
    [Min(0f)] public float respawnTimeReduction = 30f;
}
