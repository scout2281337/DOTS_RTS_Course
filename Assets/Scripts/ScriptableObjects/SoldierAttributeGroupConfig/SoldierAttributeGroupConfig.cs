using UnityEngine;

[CreateAssetMenu(fileName = "SoldierAttributeGroupConfig", menuName = "Scriptable Objects/Soldier/SoldierAttributeGroupConfig")]
public class SoldierAttributeGroupConfig : ScriptableObject
{
    public BodyConfig bodyConfig;
    public WeaponConfig[] weaponConfigs;
    public SkillConfig[] skillConfigs;
}
