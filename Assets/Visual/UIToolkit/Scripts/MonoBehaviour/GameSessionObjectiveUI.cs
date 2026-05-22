using UnityEngine;
using UnityEngine.UIElements;

public class GameSessionObjectiveUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet styleSheet;

    [Header("Layout")]
    [SerializeField] private float panelTop = 374f;
    [SerializeField] private float panelRight = 26f;
    [SerializeField] private float panelWidth = 310f;

    [Header("Refresh")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.1f;

    private GameSessionManager sessionManager;
    private VisualElement panel;
    private VisualElement progressFill;
    private Label stateLabel;
    private Label titleLabel;
    private Label descriptionLabel;
    private Label progressLabel;
    private Label threatLabel;
    private bool subscribed;
    private float refreshTimer;

    private void Start()
    {
        BuildUI();
        TryBindSessionManager();
        RefreshImmediate();
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

        TryBindSessionManager();

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        RefreshImmediate();
    }

    private void BuildUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogWarning("GameSessionObjectiveUI needs a UIDocument in the scene.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (styleSheet == null)
            styleSheet = Resources.Load<StyleSheet>("UI/Minimap");

        if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            root.styleSheets.Add(styleSheet);

        panel?.RemoveFromHierarchy();

        panel = new VisualElement();
        panel.AddToClassList("session-objective-panel");
        panel.style.position = Position.Absolute;
        panel.style.top = panelTop;
        panel.style.right = panelRight;
        panel.style.width = panelWidth;
        panel.pickingMode = PickingMode.Ignore;
        root.Add(panel);

        VisualElement header = new VisualElement();
        header.AddToClassList("session-objective-header");
        panel.Add(header);

        stateLabel = new Label("MISSION");
        stateLabel.AddToClassList("session-objective-kicker");
        header.Add(stateLabel);

        threatLabel = new Label("THREAT 0");
        threatLabel.AddToClassList("session-objective-threat");
        header.Add(threatLabel);

        titleLabel = new Label("NO ACTIVE OBJECTIVE");
        titleLabel.AddToClassList("session-objective-title");
        panel.Add(titleLabel);

        descriptionLabel = new Label();
        descriptionLabel.AddToClassList("session-objective-description");
        panel.Add(descriptionLabel);

        VisualElement progressRow = new VisualElement();
        progressRow.AddToClassList("session-objective-progress-row");
        panel.Add(progressRow);

        VisualElement progressTrack = new VisualElement();
        progressTrack.AddToClassList("session-objective-progress-track");
        progressRow.Add(progressTrack);

        progressFill = new VisualElement();
        progressFill.AddToClassList("session-objective-progress-fill");
        progressTrack.Add(progressFill);

        progressLabel = new Label();
        progressLabel.AddToClassList("session-objective-progress-label");
        progressRow.Add(progressLabel);
    }

    private void TryBindSessionManager()
    {
        if (sessionManager != null)
            return;

        sessionManager = FindFirstObjectByType<GameSessionManager>();
        if (sessionManager == null)
            return;

        sessionManager.OnStateChanged += OnSessionChanged;
        sessionManager.OnTaskStarted += OnTaskStarted;
        sessionManager.OnTaskCompleted += OnTaskCompleted;
        sessionManager.OnThreatChanged += OnThreatChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || sessionManager == null)
            return;

        sessionManager.OnStateChanged -= OnSessionChanged;
        sessionManager.OnTaskStarted -= OnTaskStarted;
        sessionManager.OnTaskCompleted -= OnTaskCompleted;
        sessionManager.OnThreatChanged -= OnThreatChanged;
        subscribed = false;
    }

    private void RefreshImmediate()
    {
        if (panel == null)
            return;

        if (sessionManager == null)
        {
            panel.style.display = DisplayStyle.None;
            return;
        }

        GameSessionState state = sessionManager.State;
        GameSessionTaskDefinition task = sessionManager.ActiveTask;

        bool hasVisibleSession = state != GameSessionState.Inactive || task != null;
        panel.style.display = hasVisibleSession ? DisplayStyle.Flex : DisplayStyle.None;
        if (!hasVisibleSession)
            return;

        ApplyStateClasses(state);

        stateLabel.text = GetStateLabel(state);
        threatLabel.text = $"THREAT {Mathf.RoundToInt(sessionManager.ThreatLevel)}";

        if (state == GameSessionState.Victory)
        {
            titleLabel.text = "MISSION COMPLETE";
            descriptionLabel.text = "Squad objective secured.";
            progressLabel.text = "DONE";
            SetProgress(1f);
            return;
        }

        if (state == GameSessionState.Defeat)
        {
            titleLabel.text = "MISSION FAILED";
            descriptionLabel.text = "Squad signal lost.";
            progressLabel.text = "LOST";
            SetProgress(1f);
            return;
        }

        if (task == null)
        {
            titleLabel.text = "NO ACTIVE OBJECTIVE";
            descriptionLabel.text = "Awaiting orders.";
            progressLabel.text = string.Empty;
            SetProgress(0f);
            return;
        }

        titleLabel.text = string.IsNullOrWhiteSpace(task.Title) ? "OBJECTIVE" : task.Title;
        descriptionLabel.text = string.IsNullOrWhiteSpace(task.Description)
            ? GetFallbackDescription(task)
            : task.Description;
        progressLabel.text = sessionManager.ActiveTaskProgressText;
        SetProgress(sessionManager.TaskProgress01);
    }

    private void SetProgress(float value)
    {
        if (progressFill == null)
            return;

        progressFill.style.width = new Length(Mathf.Clamp01(value) * 100f, LengthUnit.Percent);
    }

    private void ApplyStateClasses(GameSessionState state)
    {
        panel.RemoveFromClassList("briefing");
        panel.RemoveFromClassList("playing");
        panel.RemoveFromClassList("extraction");
        panel.RemoveFromClassList("victory");
        panel.RemoveFromClassList("defeat");
        panel.AddToClassList(state.ToString().ToLowerInvariant());
    }

    private static string GetStateLabel(GameSessionState state)
    {
        return state switch
        {
            GameSessionState.Briefing => "BRIEFING",
            GameSessionState.Playing => "MISSION",
            GameSessionState.Extraction => "EXTRACTION",
            GameSessionState.Victory => "COMPLETE",
            GameSessionState.Defeat => "FAILED",
            _ => "MISSION"
        };
    }

    private static string GetFallbackDescription(GameSessionTaskDefinition task)
    {
        return task.Type switch
        {
            GameSessionTaskType.KillEnemies => $"Eliminate hostiles: {task.RequiredKillCount}.",
            GameSessionTaskType.ReachArea => "Move the squad into the marked area.",
            GameSessionTaskType.SurviveTime => "Hold position until the timer expires.",
            GameSessionTaskType.ActivateGenerator => "Find and activate the generator.",
            GameSessionTaskType.EscortPlatform => "Stay near the platform and escort it to the destination.",
            GameSessionTaskType.Extract => "Reach the extraction zone.",
            GameSessionTaskType.Briefing => "Stand by for mission details.",
            GameSessionTaskType.WaitTime => "Await further instructions.",
            _ => "Follow mission instructions."
        };
    }

    private void OnSessionChanged(GameSessionState _)
    {
        RefreshImmediate();
    }

    private void OnTaskStarted(GameSessionTaskDefinition _)
    {
        RefreshImmediate();
    }

    private void OnTaskCompleted(GameSessionTaskDefinition _)
    {
        RefreshImmediate();
    }

    private void OnThreatChanged(float _)
    {
        RefreshImmediate();
    }
}
