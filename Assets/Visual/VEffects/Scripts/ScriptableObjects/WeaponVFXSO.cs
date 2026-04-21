using UnityEngine;

[CreateAssetMenu(fileName = "WeaponVFXSO", menuName = "Scriptable Objects/VFX/WeaponVFXSO")]
public class WeaponVFXSO : ScriptableObject
{
    public VFXObject MuzzleFlashVFXObject;
    public VFXObject ExplosionVFXObject;
    public VFXObject BloodBurstVFXObject;
    public VFXObject SparkBurstVFXObject;
}