using UnityEngine;
using UnityEngine.UIElements;


public abstract class BaseSoldierAttribute : ScriptableObject
{
    [Header("Base")] 
    public string attributeName;
    [TextArea] public string attributeDescription;


    public virtual void GetAttributeLobbyBox(out Button attributeButton, out Label attributeDescription, out VisualElement miscBox)
    {
        attributeButton = UITK.CreateElement<Button>("RigidButton", "attributeButton");
        attributeButton.text = attributeName;

        attributeDescription = UITK.CreateElement<Label>("attributeDescription");
        attributeDescription.text = this.attributeDescription;

        miscBox = UITK.CreateElement("miscBox");
    }
}