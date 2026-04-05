using UnityEngine;
using UnityEngine.UIElements;

public abstract class BodyConfig : BaseSoldierAttribute, IInfoBlockUI
{
    [Header("Armor")]
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
                armorBar.lowValue = 0;
                armorBar.highValue = 3;
                armorBar.value = armor;
                
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

        var armorLabel = UITK.AddElement<Label>(horizontalPair, "armorLabel");
        armorLabel.text = "Броня: " + armor;

        var speedLabel = UITK.AddElement<Label>(horizontalPair, "speedLabel");
        speedLabel.text = "Скорость: " + speed;


        attributeDescription.AddToClassList("DP");
    }
}