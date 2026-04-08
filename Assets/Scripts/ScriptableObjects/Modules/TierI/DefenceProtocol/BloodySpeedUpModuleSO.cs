using UnityEngine;

[CreateAssetMenu(fileName = "BloodySpeedUpModuleSO", menuName = "Scriptable Objects/Modules/TierI/BloodySpeedUpModuleSO")]
public class BloodySpeedUpModuleSO : ModuleBaseSO
{
    [Min(0f)] public float moveSpeedBonus = 1f;
    [Min(0f)] public float stackResetDelay = 7f;
    [Min(1)] public int maxStacks = 30;
}
