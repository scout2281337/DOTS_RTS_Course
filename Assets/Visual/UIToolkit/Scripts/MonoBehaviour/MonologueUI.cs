using UnityEngine;
using UnityEngine.UIElements;

public class MonologueUI : MonoBehaviour
{
    private enum PanelPlacement
    {
        BottomLeft,
        Center
    }

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet styleSheet;

    [Header("Layout")]
    [SerializeField] private PanelPlacement placement = PanelPlacement.Center;
    [SerializeField] private float panelLeft = 34f;
    [SerializeField] private float panelBottom = 34f;
    [SerializeField] private float panelWidth = 560f;

    [Header("Text")]
    [SerializeField, Min(1f)] private float typewriterCharactersPerSecond = 58f;

    private MonologueManager manager;
    private VisualElement panel;
    private VisualElement progressFill;
    private Label speakerLabel;
    private Label channelLabel;
    private Label messageLabel;
    private Label hintLabel;
    private Button continueButton;
    private string fullText = string.Empty;
    private float typewriterTimer;
    private bool subscribed;
    private bool showingLine;

    private void Start()
    {
        BuildUI();
        TryBindManager();
        Hide();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        panel?.RemoveFromHierarchy();
    }

    private void Update()
    {
        if (panel == null)
            BuildUI();

        TryBindManager();
        UpdateTypewriter();
    }

    private void BuildUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogWarning($"{nameof(MonologueUI)} needs a UIDocument in the scene.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (styleSheet == null)
            styleSheet = Resources.Load<StyleSheet>("UI/Monologue");

        if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            root.styleSheets.Add(styleSheet);

        panel?.RemoveFromHierarchy();

        panel = new VisualElement();
        panel.AddToClassList("monologue-panel");
        panel.style.position = Position.Absolute;
        panel.style.width = panelWidth;
        panel.pickingMode = PickingMode.Position;
        ApplyPlacement();
        root.Add(panel);

        VisualElement scan = new VisualElement();
        scan.AddToClassList("monologue-scan");
        panel.Add(scan);

        VisualElement header = new VisualElement();
        header.AddToClassList("monologue-header");
        panel.Add(header);

        speakerLabel = new Label("COMMAND");
        speakerLabel.AddToClassList("monologue-speaker");
        header.Add(speakerLabel);

        channelLabel = new Label("TACTICAL RADIO");
        channelLabel.AddToClassList("monologue-channel");
        header.Add(channelLabel);

        messageLabel = new Label();
        messageLabel.AddToClassList("monologue-message");
        panel.Add(messageLabel);

        VisualElement footer = new VisualElement();
        footer.AddToClassList("monologue-footer");
        panel.Add(footer);

        hintLabel = new Label();
        hintLabel.AddToClassList("monologue-hint");
        footer.Add(hintLabel);

        continueButton = new Button(OnContinueClicked)
        {
            text = "ДАЛЬШЕ"
        };
        continueButton.AddToClassList("monologue-continue");
        footer.Add(continueButton);

        VisualElement progressTrack = new VisualElement();
        progressTrack.AddToClassList("monologue-progress-track");
        panel.Add(progressTrack);

        progressFill = new VisualElement();
        progressFill.AddToClassList("monologue-progress-fill");
        progressTrack.Add(progressFill);
    }

    private void ApplyPlacement()
    {
        if (panel == null)
            return;

        panel.style.left = StyleKeyword.Null;
        panel.style.right = StyleKeyword.Null;
        panel.style.top = StyleKeyword.Null;
        panel.style.bottom = StyleKeyword.Null;
        panel.style.translate = new Translate(0f, 0f, 0f);

        if (placement == PanelPlacement.Center)
        {
            panel.style.left = new Length(50f, LengthUnit.Percent);
            panel.style.top = new Length(50f, LengthUnit.Percent);
            panel.style.translate = new Translate(new Length(-50f, LengthUnit.Percent), new Length(-50f, LengthUnit.Percent), 0f);
            return;
        }

        panel.style.left = panelLeft;
        panel.style.bottom = panelBottom;
    }

    private void TryBindManager()
    {
        if (subscribed && manager != null)
            return;

        manager = MonologueManager.Instance;
        if (manager == null)
            manager = FindFirstObjectByType<MonologueManager>();

        if (manager == null)
            return;

        manager.OnLineStarted += OnLineStarted;
        manager.OnLineProgressChanged += OnLineProgressChanged;
        manager.OnHidden += OnHidden;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || manager == null)
            return;

        manager.OnLineStarted -= OnLineStarted;
        manager.OnLineProgressChanged -= OnLineProgressChanged;
        manager.OnHidden -= OnHidden;
        subscribed = false;
    }

    private void OnLineStarted(MonologueLine line, int index, int total)
    {
        if (panel == null)
            BuildUI();

        if (panel == null)
            return;

        showingLine = true;
        fullText = line.Text ?? string.Empty;
        typewriterTimer = 0f;

        ApplyMood(line.Mood);
        panel.style.display = DisplayStyle.Flex;
        speakerLabel.text = string.IsNullOrWhiteSpace(line.Speaker) ? "COMMAND" : line.Speaker;
        channelLabel.text = $"{GetMoodLabel(line.Mood)} // {index:00}/{Mathf.Max(1, total):00}";
        messageLabel.text = string.Empty;
        hintLabel.text = string.IsNullOrWhiteSpace(line.Hint) ? "Ожидайте приказ..." : line.Hint;
        continueButton.style.display = line.WaitForClick ? DisplayStyle.Flex : DisplayStyle.None;
        SetProgress(0f);
    }

    private void OnLineProgressChanged(float progress)
    {
        SetProgress(progress);
    }

    private void OnHidden()
    {
        Hide();
    }

    private void UpdateTypewriter()
    {
        if (!showingLine || messageLabel == null)
            return;

        typewriterTimer += Time.deltaTime;
        int visibleCharacters = Mathf.Clamp(
            Mathf.FloorToInt(typewriterTimer * typewriterCharactersPerSecond),
            0,
            fullText.Length);

        messageLabel.text = fullText.Substring(0, visibleCharacters);
    }

    private void Hide()
    {
        showingLine = false;
        fullText = string.Empty;

        if (panel != null)
            panel.style.display = DisplayStyle.None;
    }

    private void SetProgress(float progress)
    {
        if (progressFill == null)
            return;

        progressFill.style.width = new Length(Mathf.Clamp01(progress) * 100f, LengthUnit.Percent);
    }

    private void ApplyMood(MonologueMood mood)
    {
        panel.RemoveFromClassList("radio");
        panel.RemoveFromClassList("info");
        panel.RemoveFromClassList("warning");
        panel.RemoveFromClassList("danger");
        panel.RemoveFromClassList("success");
        panel.AddToClassList(mood.ToString().ToLowerInvariant());
    }

    private void OnContinueClicked()
    {
        if (manager == null)
            return;

        if (messageLabel != null && messageLabel.text != fullText)
        {
            messageLabel.text = fullText;
            typewriterTimer = fullText.Length / Mathf.Max(1f, typewriterCharactersPerSecond);
            return;
        }

        manager.Advance();
    }

    private static string GetMoodLabel(MonologueMood mood)
    {
        return mood switch
        {
            MonologueMood.Info => "INFO",
            MonologueMood.Warning => "WARNING",
            MonologueMood.Danger => "DANGER",
            MonologueMood.Success => "CONFIRMED",
            _ => "RADIO"
        };
    }
}
