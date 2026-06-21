using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class GameplayPauseMenuUI : MonoBehaviour
{
    private const string PauseRootName = "gameplay-pause-root";

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;

    [Header("Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Scene Exit")]
    [SerializeField] private string loadingScreen = SceneDirector.LOADINGSCREEN;
    [SerializeField] private string[] mainMenuScenes = { "MainMenuLobby", "City" };

    private VisualElement pauseRoot;
    private VisualElement settingsOverlay;
    private GameSettingsPanel settingsPanel;
    private bool isOpen;
    private bool settingsOpen;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        BuildMenu();
    }

    private void Start()
    {
        settingsPanel?.ApplySavedSettings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleMenu();
    }

    private void BuildMenu()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning($"{nameof(GameplayPauseMenuUI)} needs a UIDocument in the scene.", this);
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        AddDefaultStyleSheets(root);
        AddStyleSheetIfMissing(root, Resources.Load<StyleSheet>("UI/GameplayPauseMenu"));

        if (styleSheets != null)
        {
            foreach (StyleSheet sheet in styleSheets)
                AddStyleSheetIfMissing(root, sheet);
        }

        VisualElement oldRoot = root.Q<VisualElement>(PauseRootName);
        oldRoot?.RemoveFromHierarchy();

        pauseRoot = UITK.AddElement(root, "gameplayPauseOverlay");
        pauseRoot.name = PauseRootName;
        pauseRoot.style.display = DisplayStyle.None;

        ApplyEssentialOverlayLayout(pauseRoot);
        BuildMainPanel(pauseRoot);
        BuildSettingsOverlay(pauseRoot);
    }

    private void BuildMainPanel(VisualElement parent)
    {
        VisualElement frame = UITK.AddElement(parent, "gameplayPauseFrame");

        Label eyebrow = UITK.AddElement<Label>(frame, "pauseEyebrow", "P3");
        eyebrow.text = "СИМУЛЯЦИЯ АКТИВНА";

        Label title = UITK.AddElement<Label>(frame, "pauseTitle", "H2");
        title.text = "Тактическое меню";

        Label subtitle = UITK.AddElement<Label>(frame, "pauseSubtitle", "P2");
        subtitle.text = "Игра не ставится на паузу: бой, зомби и события продолжаются на фоне.";

        VisualElement telemetry = UITK.AddElement(frame, "pauseTelemetry");
        AddTelemetryChip(telemetry, "TIMEFLOW", "LIVE");
        AddTelemetryChip(telemetry, "SQUAD LINK", "ONLINE");
        AddTelemetryChip(telemetry, "ESC", "CLOSE");

        VisualElement actions = UITK.AddElement(frame, "pauseActions");

        Button resume = UITK.AddElement<Button>(actions, "PrimaryButton", "RigidButton", "H3", "pauseButton");
        resume.text = "Продолжить";
        resume.clicked += CloseMenu;

        Button settings = UITK.AddElement<Button>(actions, "SecondaryButton", "RigidButton", "H3", "pauseButton");
        settings.text = "Настройки";
        settings.clicked += OpenSettings;

        Button exitToMenu = UITK.AddElement<Button>(actions, "TertiaryButton", "RigidButton", "H3", "pauseButton", "dangerButton");
        exitToMenu.text = "В меню";
        exitToMenu.clicked += ExitToMainMenu;

        Label hint = UITK.AddElement<Label>(frame, "pauseHint", "P3");
        hint.text = "Совет: открытое меню блокирует клики по карте, но не останавливает саму миссию.";
    }

    private void BuildSettingsOverlay(VisualElement parent)
    {
        settingsOverlay = UITK.AddElement(parent, "settingsOverlay", "gameplaySettingsOverlay");
        settingsOverlay.style.display = DisplayStyle.None;

        settingsPanel = new GameSettingsPanel(audioMixer);
        settingsPanel.Build(
            settingsOverlay,
            "Настройки",
            "Изменения применяются сразу. Симуляция на фоне не останавливается.",
            "Назад",
            CloseSettings);
    }

    private void AddTelemetryChip(VisualElement parent, string label, string value)
    {
        VisualElement chip = UITK.AddElement(parent, "pauseChip");

        Label labelElement = UITK.AddElement<Label>(chip, "pauseChipLabel", "P4");
        labelElement.text = label;

        Label valueElement = UITK.AddElement<Label>(chip, "pauseChipValue", "P3");
        valueElement.text = value;
    }

    private void ToggleMenu()
    {
        if (settingsOpen)
        {
            CloseSettings();
            return;
        }

        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        if (pauseRoot == null)
            return;

        isOpen = true;
        pauseRoot.style.display = DisplayStyle.Flex;
        pauseRoot.BringToFront();
    }

    private void CloseMenu()
    {
        if (pauseRoot == null)
            return;

        CloseSettings();
        isOpen = false;
        pauseRoot.style.display = DisplayStyle.None;
    }

    private void OpenSettings()
    {
        if (settingsOverlay == null)
            return;

        settingsOpen = true;
        settingsOverlay.style.display = DisplayStyle.Flex;
        settingsOverlay.BringToFront();
    }

    private void CloseSettings()
    {
        settingsOpen = false;

        if (settingsOverlay != null)
            settingsOverlay.style.display = DisplayStyle.None;
    }

    private void ExitToMainMenu()
    {
        CloseMenu();

        string screen = string.IsNullOrWhiteSpace(loadingScreen)
            ? SceneDirector.LOADINGSCREEN
            : loadingScreen;

        string[] scenes = mainMenuScenes != null && mainMenuScenes.Length > 0
            ? mainMenuScenes
            : new[] { SceneDirector.MAINMENU, SceneDirector.CITY };

        SceneDirector.OpenScenesThroughLoadingScreen(screen, scenes);
    }

    private static void AddDefaultStyleSheets(VisualElement root)
    {
        ViewController uiController = ViewController.Instance;
        if (uiController == null || uiController.DefaultStyleSheet == null || uiController.DefaultStyleSheet.BaseStyles == null)
            return;

        foreach (StyleSheet sheet in uiController.DefaultStyleSheet.BaseStyles)
            AddStyleSheetIfMissing(root, sheet);
    }

    private static void AddStyleSheetIfMissing(VisualElement root, StyleSheet sheet)
    {
        if (root == null || sheet == null || root.styleSheets.Contains(sheet))
            return;

        root.styleSheets.Add(sheet);
    }

    private static void ApplyEssentialOverlayLayout(VisualElement overlay)
    {
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.top = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
    }
}
