using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private CinemachineCamera _lobbyCamera;

    private VisualElement _mainMenu;


    private void BuildMainMenu()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        root.Clear();

        ViewController _UIController = ViewController.Instance;
        foreach (StyleSheet sheet in _UIController.DefaultStyleSheet.BaseStyles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in _styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        _mainMenu = UITK.AddElement(root, "mainMenu");

        var bottomSection = UITK.AddElement(_mainMenu, "bottomSection");

        var menu = UITK.AddElement(bottomSection, "menu");
        
            var start = UITK.AddElement<Button>(menu, "PrimaryButton", "RigidButton", "H3", "menuButton", "start");
            start.text = "Высадка";
            start.clicked += () => {
                _lobbyCamera.Priority += 2;
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
        UITK.TrackUIToWorldPosition(transform.position, _mainMenu, Camera.main, new Vector2(0, 0));
    }
}
