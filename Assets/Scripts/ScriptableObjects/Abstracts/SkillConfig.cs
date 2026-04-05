using UnityEngine;
using UnityEngine.UIElements;

public abstract class SkillConfig : BaseSoldierAttribute, IInfoBlockUI
{
    public float power;
    public float cooldown;
    public float duration;
    public float range;
    public float area;

    public virtual VisualElement SetInfoBlockUI(VisualElement infoPanel)
    {
        var skillDetails = UITK.AddElement(infoPanel, "skillDetails", "infoBlock");

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

    public override void GetAttributeLobbyBox(out Button attributeButton, out Label attributeDescription, out VisualElement statsBox)
    {
        base.GetAttributeLobbyBox(out attributeButton, out attributeDescription, out statsBox);

        attributeButton.AddToClassList("TS");


        statsBox.AddToClassList("TS");

        var powerLabel = UITK.AddElement<Label>(statsBox, "P1", "powerLabel");
        powerLabel.text = "Мощь: " + power;

        var horizontalPair = UITK.AddElement(statsBox, "horizontalPair");

        var cooldownDurationColumn = UITK.AddElement(horizontalPair, "statsColumn");
        
        var cooldownLabel = UITK.AddElement<Label>(cooldownDurationColumn, "cooldownLabel");
        cooldownLabel.text = "Перезарядка: " + cooldown;

        if(duration > 0)
        {
            var durationLabel = UITK.AddElement<Label>(cooldownDurationColumn, "durationLabel");
            durationLabel.text = "Длительность: " + duration;
        }

        var rangeAreaColumn = UITK.AddElement(horizontalPair, "statsColumn");

        if (range > 0)
        {
            var rangeLabel = UITK.AddElement<Label>(rangeAreaColumn, "rangeLabel");
            rangeLabel.text = "Дальность: " + range;
        }

        if (area > 0)
        {
            var areaLabel = UITK.AddElement<Label>(rangeAreaColumn, "areaLabel");
            areaLabel.text = "Область: " + area;
        }


        attributeDescription.AddToClassList("TS");
    }
}
