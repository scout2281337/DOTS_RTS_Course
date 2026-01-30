using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "Scriptable Objects/Classes/SkillConfig")]
public abstract class SkillConfig : ScriptableObject, IInfoBlockUI
{
    public float power;
    public float cooldown;
    public float duration;
    public float range;
    public float area;
    [Header("Optional (Damage / Range / other)")] public float PerkParameter1;
    [Header("Optional (Damage / Range / other)")] public float PerkParameter2;

    public virtual VisualElement SetInfoBlockUI(VisualElement infoPanel, ColorSchemeSO colorScheme)
    {
        var skillDetails = UITK.AddElement(infoPanel, "skillDetails", "infoBlock");
        skillDetails.style.color = colorScheme.white;

            var cooldownRow = UITK.AddElement(skillDetails, "cooldownRow", "detailRow");

                var cooldownText = UITK.AddElement<Label>(cooldownRow, "cooldownText", "detailText");
                cooldownText.text = "Cooldown:";

                var cooldownAmount = UITK.AddElement<Label>(cooldownRow, "cooldownAmount", "detailAmount"); 
                cooldownAmount.text = cooldown.ToString();

            var durationRow = UITK.AddElement(skillDetails, "durationRow", "detailRow");
            
                var durationText = UITK.AddElement<Label>(durationRow, "durationText", "detailText");
                durationText.text = "Duration:";
                
                var durationAmount = UITK.AddElement<Label>(durationRow, "durationAmount", "detailAmount");
                durationAmount.text = duration.ToString();

            var effectivenessRow = UITK.AddElement(skillDetails, "effectivenessRow", "detailRow");

                var effectivenessText = UITK.AddElement<Label>(effectivenessRow, "effectivenessText", "detailText");
                effectivenessText.text = "Effectiveness:";

                var effectivenessAmount = UITK.AddElement<Label>(effectivenessRow, "effectivenessAmount", "detailAmount");
                effectivenessAmount.text = power.ToString();

        return skillDetails;
    }
}
