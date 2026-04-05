using UnityEngine;
using UnityEngine.UIElements;

public class UITester : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;

    public Texture2D overlayImage;

    private void BuildUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        var overlay = UITK.AddElement(root, "overlay");
        overlay.style.width = 1920;
        overlay.style.height = 1080;
        overlay.style.backgroundImage = overlayImage;
        overlay.style.opacity = 0.5f;
    }


    private void Awake()
    {
        BuildUI();
    }
}
