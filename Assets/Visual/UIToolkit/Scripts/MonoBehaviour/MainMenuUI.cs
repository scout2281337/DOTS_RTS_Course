using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private UIControllerMediator UIController;

    private VisualElement mainMenu;

    private void BuildMainMenu()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in UIController.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        mainMenu = UITK.AddElement(root, "mainMenu");

        var bottomSection = UITK.AddElement(mainMenu, "bottomSection");

        var menu = UITK.AddElement(bottomSection, "menu");
        
            var start = UITK.AddElement<Button>(menu, "PrimaryButton", "RigidButton", "H3", "menuButton", "start");
            start.text = "Высадка";
            start.clicked += () => {
                StartCoroutine(CameraMotion.MoveCameraToPoint(new(0, 0, -25), 1.2f));
            };

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

    private void Update()
    {
        UITK.TrackUIToWorldPosition(transform.position, mainMenu, Camera.main, new Vector2(0, 0));
    }
}
