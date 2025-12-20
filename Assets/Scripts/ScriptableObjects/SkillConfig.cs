using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "Scriptable Objects/Classes/SkillConfig")]
public class SkillConfig : ScriptableObject, IInfoBlockUI
{
    public float cooldown;
    public float duration;
    public float speedBoost;

    public virtual VisualElement SetInfoBlockUI(VisualElement infoPanel)
    {
        var skillDetails = UITK.AddElement(infoPanel, "skillDetails", "infoBlock");

            var cooldownRow = UITK.AddElement(skillDetails, "cooldownRow", "detailRow");

                var cooldownText = UITK.AddElement<Label>(cooldownRow, "cooldownText", "detailText");
                cooldownText.text = "Cooldown";

                var cooldownAmount = UITK.AddElement<Label>(cooldownRow, "cooldownAmount", "detailAmount");
                cooldownAmount.text = cooldown.ToString();

        return skillDetails;
    }
}
