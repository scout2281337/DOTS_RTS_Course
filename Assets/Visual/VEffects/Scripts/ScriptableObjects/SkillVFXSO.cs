using UnityEngine;

[CreateAssetMenu(fileName = "SkillVFXSO", menuName = "Scriptable Objects/VFX/SkillVFXSO")]
public class SkillVFXSO : ScriptableObject
{
    public VFXObject PointerVFXObject;
    public VFXObject RangeVFXObject;
    public VFXObject LineVFXObject;
    public VFXObject StimVFXObject;
    public VFXObject BarricadeVFXObject;
    public VFXObject GaussVFXObject;
    public VFXObject ScorcherVFXObject;
}
