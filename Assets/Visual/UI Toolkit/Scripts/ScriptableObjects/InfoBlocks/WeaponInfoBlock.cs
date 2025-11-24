using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "WeaponInfoBlock", menuName = "Scriptable Objects/InfoBlock/WeaponInfoBlock")]
public class WeaponInfoBlock : InfoBlock
{
    public int armor;
    public int speed;

    public override VisualElement SetInfoBlock(VisualElement infoPanel)
    {
        var classDetails = UITK.AddElement(infoPanel, "classDetails", "infoBlock");

        var armorBox = UITK.AddElement(classDetails, "armorBox");

        var armorText = UITK.AddElement<Label>(armorBox, "armorText");

        var armorBar = UITK.AddElement<ProgressBar>(armorBox, "armorBar");

        var speedBox = UITK.AddElement(classDetails, "speedBox");

        var speedText = UITK.AddElement<Label>(speedBox, "speedText");

        var speedBar = UITK.AddElement<ProgressBar>(speedBox, "speedBar");

        return classDetails;
    }
}
