using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;


    private void InitializeUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        var UICtrlMng = UIControllerManager.Instance;

        foreach (StyleSheet sheet in UICtrlMng.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        var menu = UITK.AddElement(root, "menu");
        
            var start = UITK.AddElement<Button>(menu, "MainButton", "menuButton", "start");
            start.text = "Start";

            var options = UITK.AddElement<Button>(menu, "MainButton", "menuButton", "options");
            options.text = "Options";

            var quit = UITK.AddElement<Button>(menu, "MainButton", "menuButton", "quit");
            quit.text = "Quit";
    }


    private void Awake()
    {
        InitializeUI();
    }
}
