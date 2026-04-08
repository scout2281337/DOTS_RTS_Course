using UnityEngine;

[CreateAssetMenu(fileName = "DoubleShellModuleSO", menuName = "Scriptable Objects/Modules/TierI/DoubleShellModuleSO")]
public class DoubleShellModuleSO : ModuleBaseSO
{
    [Min(0f)] public float damageThreshold = 15f;
}
