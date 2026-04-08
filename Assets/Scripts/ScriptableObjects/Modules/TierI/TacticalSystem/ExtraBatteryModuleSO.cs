using UnityEngine;

[CreateAssetMenu(fileName = "ExtraBatteryModuleSO", menuName = "Scriptable Objects/Modules/TierI/ExtraBatteryModuleSO")]
public class ExtraBatteryModuleSO : ModuleBaseSO
{
    [Min(1)] public int additionalCharges = 1;
}
