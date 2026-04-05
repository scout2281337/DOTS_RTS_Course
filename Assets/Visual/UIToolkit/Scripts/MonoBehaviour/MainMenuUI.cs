using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;


    private void BuildMainMenu()
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

        var bottomSection = UITK.AddElement(root, "bottomSection");

        var menu = UITK.AddElement(bottomSection, "menu");
        
            var start = UITK.AddElement<Button>(menu, "PrimaryButton", "RigidButton", "H3", "menuButton", "start");
            start.text = "Высадка";

            var collection = UITK.AddElement<Button>(menu, "SecondaryButton", "RigidButton", "H3", "menuButton", "start");
            collection.text = "Коллекция";

            var options = UITK.AddElement<Button>(menu, "TertiaryButton", "RigidButton", "H3", "menuButton", "options");
            options.text = "Настройки";

            var quit = UITK.AddElement<Button>(menu, "TertiaryButton", "RigidButton", "H3", "menuButton", "quit");
            quit.text = "Выйти";
    }


    private void Awake()
    {
        BuildMainMenu();
    }
}
