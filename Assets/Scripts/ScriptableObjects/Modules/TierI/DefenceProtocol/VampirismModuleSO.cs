using UnityEngine;

[CreateAssetMenu(fileName = "VampirismModuleSO", menuName = "Scriptable Objects/Modules/TierI/VampirismModuleSO")]
public class VampirismModuleSO : ModuleBaseSO
{
    [Min(0f)] public float killRadius = 3f;
    [Min(0f)] public float healAmount = 1f;
}
