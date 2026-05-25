using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string QualityKey = "Settings.Quality";

    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SFXVolume";

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private CinemachineCamera _lobbyCamera;

    [Header("Settings")]
    [SerializeField] private AudioMixer _audioMixer;

    private VisualElement _mainMenu;
    private VisualElement _settingsOverlay;
    private Label _qualityValueLabel;
    private int _qualityIndex;

    private void Awake()
    {
        BuildMainMenu();
    }

    private void Start()
    {
        ApplySavedSettings();
    }

    private void Update()
    {
        if (_mainMenu == null || Camera.main == null)
            return;

        UITK.TrackUIToWorldPosition(transform.position, _mainMenu, Camera.main, Vector2.zero);
    }

    private void BuildMainMenu()
    {
        if (_uiDocument == null)
        {
            Debug.LogError($"{nameof(MainMenuUI)} has no UIDocument assigned.", this);
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;
        root.Clear();

        ViewController uiController = ViewController.Instance;
        if (uiController != null && uiController.DefaultStyleSheet != null && uiController.DefaultStyleSheet.BaseStyles != null)
        {
            foreach (StyleSheet sheet in uiController.DefaultStyleSheet.BaseStyles)
            {
                AddStyleSheetIfMissing(root, sheet);
            }
        }

        if (_styleSheets != null)
        {
            foreach (StyleSheet sheet in _styleSheets)
            {
                AddStyleSheetIfMissing(root, sheet);
            }
        }

        _mainMenu = UITK.AddElement(root, "mainMenu");

        VisualElement bottomSection = UITK.AddElement(_mainMenu, "bottomSection");
        VisualElement menu = UITK.AddElement(bottomSection, "menu");

        Button start = UITK.AddElement<Button>(menu, "PrimaryButton", "RigidButton", "H3", "menuButton", "start");
        start.text = "Высадка";
        start.clicked += OnStartClicked;

        Button collection = UITK.AddElement<Button>(menu, "SecondaryButton", "RigidButton", "H3", "menuButton", "collection");
        collection.text = "Коллекция";

        Button options = UITK.AddElement<Button>(menu, "TertiaryButton", "RigidButton", "H3", "menuButton", "options");
        options.text = "Настройки";
        options.clicked += OpenSettings;

        Button quit = UITK.AddElement<Button>(menu, "TertiaryButton", "RigidButton", "H3", "menuButton", "quit");
        quit.text = "Выйти";
        quit.clicked += QuitGame;

        BuildSettingsPanel(root);
    }

    private void BuildSettingsPanel(VisualElement parent)
    {
        _settingsOverlay = UITK.AddElement(parent, "settingsOverlay");
        _settingsOverlay.style.display = DisplayStyle.None;

        VisualElement panel = UITK.AddElement(_settingsOverlay, "settingsPanel");

        Label title = UITK.AddElement<Label>(panel, "settingsTitle", "H2");
        title.text = "Настройки";

        Label subtitle = UITK.AddElement<Label>(panel, "settingsSubtitle", "P2");
        subtitle.text = "Быстрая настройка перед вылетом";

        AddSliderSetting(panel, "Общая громкость", PlayerPrefs.GetFloat(MasterVolumeKey, 1f), value =>
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            ApplyMasterVolume(value);
        });

        AddSliderSetting(panel, "Музыка", PlayerPrefs.GetFloat(MusicVolumeKey, 1f), value =>
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            ApplyMixerVolume(MusicVolumeParameter, value);
        });

        AddSliderSetting(panel, "Эффекты", PlayerPrefs.GetFloat(SfxVolumeKey, 1f), value =>
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            ApplyMixerVolume(SfxVolumeParameter, value);
        });

        AddToggleSetting(panel, "Полный экран", PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1, isEnabled =>
        {
            PlayerPrefs.SetInt(FullscreenKey, isEnabled ? 1 : 0);
            Screen.fullScreen = isEnabled;
        });

        AddQualitySetting(panel);

        VisualElement actions = UITK.AddElement(panel, "settingsActions");

        Button reset = UITK.AddElement<Button>(actions, "SecondaryButton", "RigidButton", "P1", "settingsButton");
        reset.text = "Сброс";
        reset.clicked += ResetSettings;

        Button close = UITK.AddElement<Button>(actions, "PrimaryButton", "RigidButton", "P1", "settingsButton");
        close.text = "Назад";
        close.clicked += CloseSettings;
    }

    private void AddSliderSetting(VisualElement parent, string title, float startValue, Action<float> onChanged)
    {
        VisualElement block = UITK.AddElement(parent, "settingsBlock");
        VisualElement header = UITK.AddElement(block, "settingsRow");

        Label titleLabel = UITK.AddElement<Label>(header, "settingsLabel", "P1");
        titleLabel.text = title;

        Label valueLabel = UITK.AddElement<Label>(header, "settingsValue", "P2");
        valueLabel.text = FormatPercent(startValue);

        Slider slider = new Slider(0f, 1f);
        slider.AddToClassList("settingsSlider");
        slider.SetValueWithoutNotify(Mathf.Clamp01(startValue));
        slider.RegisterValueChangedCallback(evt =>
        {
            float value = Mathf.Clamp01(evt.newValue);
            valueLabel.text = FormatPercent(value);
            onChanged?.Invoke(value);
            PlayerPrefs.Save();
        });

        block.Add(slider);
    }

    private void AddToggleSetting(VisualElement parent, string title, bool startValue, Action<bool> onChanged)
    {
        VisualElement row = UITK.AddElement(parent, "settingsRow", "settingsToggleRow");

        Label titleLabel = UITK.AddElement<Label>(row, "settingsLabel", "P1");
        titleLabel.text = title;

        Toggle toggle = UITK.AddElement<Toggle>(row, "settingsToggle");
        toggle.SetValueWithoutNotify(startValue);
        toggle.RegisterValueChangedCallback(evt =>
        {
            onChanged?.Invoke(evt.newValue);
            PlayerPrefs.Save();
        });
    }

    private void AddQualitySetting(VisualElement parent)
    {
        VisualElement block = UITK.AddElement(parent, "settingsBlock");
        VisualElement row = UITK.AddElement(block, "settingsRow");

        Label titleLabel = UITK.AddElement<Label>(row, "settingsLabel", "P1");
        titleLabel.text = "Качество";

        VisualElement controls = UITK.AddElement(row, "qualityControls");

        Button previous = UITK.AddElement<Button>(controls, "TertiaryButton", "RigidButton", "P1", "qualityButton");
        previous.text = "<";
        previous.clicked += () => ChangeQuality(-1);

        _qualityValueLabel = UITK.AddElement<Label>(controls, "settingsValue", "qualityValue", "P2");

        Button next = UITK.AddElement<Button>(controls, "TertiaryButton", "RigidButton", "P1", "qualityButton");
        next.text = ">";
        next.clicked += () => ChangeQuality(1);

        string[] names = QualitySettings.names;
        int savedQuality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        _qualityIndex = names == null || names.Length == 0 ? 0 : Mathf.Clamp(savedQuality, 0, names.Length - 1);
        UpdateQualityLabel();
    }

    private void OnStartClicked()
    {
        if (_lobbyCamera != null)
            _lobbyCamera.Priority += 2;
    }

    private void OpenSettings()
    {
        if (_settingsOverlay == null)
            return;

        _settingsOverlay.style.display = DisplayStyle.Flex;
        _settingsOverlay.BringToFront();
    }

    private void CloseSettings()
    {
        if (_settingsOverlay == null)
            return;

        _settingsOverlay.style.display = DisplayStyle.None;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetSettings()
    {
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.DeleteKey(SfxVolumeKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(QualityKey);

        PlayerPrefs.Save();
        BuildMainMenu();
        ApplySavedSettings();
        OpenSettings();
    }

    private void ApplySavedSettings()
    {
        ApplyMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        ApplyMixerVolume(MusicVolumeParameter, PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        ApplyMixerVolume(SfxVolumeParameter, PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        string[] names = QualitySettings.names;
        if (names != null && names.Length > 0)
        {
            int savedQuality = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, names.Length - 1);
            QualitySettings.SetQualityLevel(savedQuality, true);
            _qualityIndex = savedQuality;
            UpdateQualityLabel();
        }
    }

    private void ChangeQuality(int direction)
    {
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            return;

        _qualityIndex = Mathf.Clamp(_qualityIndex + direction, 0, names.Length - 1);
        QualitySettings.SetQualityLevel(_qualityIndex, true);
        PlayerPrefs.SetInt(QualityKey, _qualityIndex);
        PlayerPrefs.Save();
        UpdateQualityLabel();
    }

    private void UpdateQualityLabel()
    {
        if (_qualityValueLabel == null)
            return;

        string[] names = QualitySettings.names;
        _qualityValueLabel.text = names == null || names.Length == 0
            ? "Default"
            : names[Mathf.Clamp(_qualityIndex, 0, names.Length - 1)];
    }

    private void ApplyMasterVolume(float value)
    {
        AudioMixer mixer = GetAudioMixer();
        if (mixer != null)
        {
            AudioListener.volume = 1f;
            mixer.SetFloat(MasterVolumeParameter, VolumeToDecibels(value));
            return;
        }

        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void ApplyMixerVolume(string parameterName, float value)
    {
        AudioMixer mixer = GetAudioMixer();
        if (mixer == null)
            return;

        mixer.SetFloat(parameterName, VolumeToDecibels(value));
    }

    private AudioMixer GetAudioMixer()
    {
        if (_audioMixer != null)
            return _audioMixer;

        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            if (source != null && source.outputAudioMixerGroup != null)
            {
                _audioMixer = source.outputAudioMixerGroup.audioMixer;
                return _audioMixer;
            }
        }

        return null;
    }

    private static float VolumeToDecibels(float value)
    {
        value = Mathf.Clamp01(value);
        return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
    }

    private static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    private static void AddStyleSheetIfMissing(VisualElement root, StyleSheet sheet)
    {
        if (root == null || sheet == null || root.styleSheets.Contains(sheet))
            return;

        root.styleSheets.Add(sheet);
    }
}
