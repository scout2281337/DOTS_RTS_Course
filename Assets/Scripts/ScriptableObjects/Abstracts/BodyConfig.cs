using UnityEngine;
using UnityEngine.UIElements;

public abstract class BodyConfig : BaseSoldierAttribute, IInfoBlockUI
{
    [Header("Armor Level (1=0%, 2=15%, 3=35%)")]
    public float armor;

    [Header("Unit mover component")]
    public int speed;
    public int rotationSpeed;

    [Header("Health component")]
    public float maxHealth;

    [Header("Find Target component")]
    public Faction targetFaction;
    public float timerMaxForOverlap;

    public UnitClass unitClass;
    public Faction currentFaction;

    public virtual VisualElement SetInfoBlockUI(VisualElement infoPanel)
    {
        var classDetails = UITK.AddElement(infoPanel, "classDetails", "infoBlock");

        var classIcon = UITK.AddElement(classDetails, "classIcon");

        var armorRow = UITK.AddElement(classDetails, "armorRow", "detailRow");

        var armorText = UITK.AddElement<Label>(armorRow, "armorText", "detailText");
        armorText.text = "Armor:";

        var armorBar = UITK.AddElement<ProgressBar>(armorRow, "armorBar", "detailBar");
        armorBar.lowValue = 1;
        armorBar.highValue = 3;
        armorBar.value = GetArmorLevel(armor);

        var speedRow = UITK.AddElement(classDetails, "speedRow", "detailRow");

        var speedText = UITK.AddElement<Label>(speedRow, "speedText", "detailText");
        speedText.text = "Speed:";

        var speedBar = UITK.AddElement<ProgressBar>(speedRow, "speedBar", "detailBar");
        speedBar.lowValue = 0;
        speedBar.highValue = 3;
        speedBar.value = speed;

        return classDetails;
    }

    public override void GetAttributeLobbyBox(out Button attributeButton, out Label attributeDescription, out VisualElement statsBox)
    {
        base.GetAttributeLobbyBox(out attributeButton, out attributeDescription, out statsBox);

        attributeButton.AddToClassList("DP");
        statsBox.AddToClassList("DP");

        var horizontalPair = UITK.AddElement(statsBox, "P1", "horizontalPair");

        int armorLevel = GetArmorLevel(armor);
        int armorReductionPercent = GetArmorReductionPercentByLevel(armorLevel);

        var armorLabel = UITK.AddElement<Label>(horizontalPair, "armorLabel");
        armorLabel.text = "Armor: " + armorLevel + " (" + armorReductionPercent + "%)";

        var speedLabel = UITK.AddElement<Label>(horizontalPair, "speedLabel");
        speedLabel.text = "Speed: " + speed;

        attributeDescription.AddToClassList("DP");
    }

    private static int GetArmorLevel(float rawLevel)
    {
        return Mathf.Clamp(Mathf.RoundToInt(rawLevel), 1, 3);
    }

    private static int GetArmorReductionPercentByLevel(int level)
    {
        return level switch
        {
            2 => 15,
            3 => 35,
            _ => 0
        };
    }
}
