using UnityEngine;

[CreateAssetMenu(fileName = "SoldierAttributesConfig", menuName = "Scriptable Objects/Soldier/SoldierAttributesConfig")]
public class SoldierAttributesConfig : ScriptableObject
{
    public Texture2D icon;
    public BodyConfig bodyConfig;
    public WeaponConfig[] weaponConfigs;
    public SkillConfig[] skillConfigs;
}
