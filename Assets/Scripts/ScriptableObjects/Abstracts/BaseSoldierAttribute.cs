using UnityEngine;
using UnityEngine.UIElements;


public abstract class BaseSoldierAttribute : ScriptableObject
{
    [Header("Base")] 
    public string attributeName;
    [TextArea] public string attributeDescription;


    public virtual void GetAttributeLobbyBox(out Button attributeButton, out Label attributeDescription, out VisualElement statsBox)
    {
        attributeButton = UITK.CreateElement<Button>("RigidButton", "L2", "attributeButton");
        attributeButton.text = attributeName;

        statsBox = UITK.CreateElement("P2", "statsBox");

        attributeDescription = UITK.CreateElement<Label>("P3", "attributeDescription");
        attributeDescription.text = this.attributeDescription;
    }
}