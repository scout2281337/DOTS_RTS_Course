using UnityEngine;
using UnityEngine.UIElements;


public abstract class BaseSoldierAttribute : ScriptableObject
{
    [Header("Base")] 
    public string attributeName;
    [TextArea] public string attributeDescription;


    public virtual void GetAttributeLobbyBox(Color BG, Color font, out Button attributeButton, out Label attributeDescription, out VisualElement miscBox)
    {
        attributeButton = UITK.CreateElement<Button>("attributeButton");
        attributeButton.style.backgroundColor = BG;
        attributeButton.style.color = font;
        attributeButton.text = attributeName;

        attributeDescription = UITK.CreateElement<Label>("attributeDescription");
        attributeDescription.style.backgroundColor = BG;
        attributeDescription.style.color = font;
        attributeDescription.text = this.attributeDescription;

        miscBox = UITK.CreateElement("miscBox");
        miscBox.style.backgroundColor = BG;
    }
}
