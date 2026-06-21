using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public sealed class GameSettingsPanel
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string QualityKey = "Settings.Quality";

    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SFXVolume";

    private readonly AudioMixer audioMixerOverride;

    private VisualElement panel;
    private Label qualityValueLabel;
    private int qualityIndex;
    private string title;
    private string subtitle;
    private string closeButtonText;
    private Action closeAction;

    public GameSettingsPanel(AudioMixer audioMixer)
    {
        audioMixerOverride = audioMixer;
    }

    public VisualElement Build(VisualElement parent, string panelTitle, string panelSubtitle, string closeText, Action onClose)
    {
        title = panelTitle;
        subtitle = panelSubtitle;
        closeButtonText = closeText;
        closeAction = onClose;

        panel = UITK.AddElement(parent, "settingsPanel");
        RebuildPanel();
        return panel;
    }

    public void ApplySavedSettings()
    {
        ApplyMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        ApplyMixerVolume(MusicVolumeParameter, PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        ApplyMixerVolume(SfxVolumeParameter, PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            return;

        int savedQuality = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, names.Length - 1);
        QualitySettings.SetQualityLevel(savedQuality, true);
        qualityIndex = savedQuality;
        UpdateQualityLabel();
    }

    private void RebuildPanel()
    {
        if (panel == null)
            return;

        panel.Clear();

        Label titleLabel = UITK.AddElement<Label>(panel, "settingsTitle", "H2");
        titleLabel.text = title;

        Label subtitleLabel = UITK.AddElement<Label>(panel, "settingsSubtitle", "P2");
        subtitleLabel.text = subtitle;

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
        close.text = closeButtonText;
        close.clicked += () => closeAction?.Invoke();
    }

    private void AddSliderSetting(VisualElement parent, string settingTitle, float startValue, Action<float> onChanged)
    {
        VisualElement block = UITK.AddElement(parent, "settingsBlock");
        VisualElement header = UITK.AddElement(block, "settingsRow");

        Label titleLabel = UITK.AddElement<Label>(header, "settingsLabel", "P1");
        titleLabel.text = settingTitle;

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

    private void AddToggleSetting(VisualElement parent, string settingTitle, bool startValue, Action<bool> onChanged)
    {
        VisualElement row = UITK.AddElement(parent, "settingsRow", "settingsToggleRow");

        Label titleLabel = UITK.AddElement<Label>(row, "settingsLabel", "P1");
        titleLabel.text = settingTitle;

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

        qualityValueLabel = UITK.AddElement<Label>(controls, "settingsValue", "qualityValue", "P2");

        Button next = UITK.AddElement<Button>(controls, "TertiaryButton", "RigidButton", "P1", "qualityButton");
        next.text = ">";
        next.clicked += () => ChangeQuality(1);

        string[] names = QualitySettings.names;
        int savedQuality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        qualityIndex = names == null || names.Length == 0 ? 0 : Mathf.Clamp(savedQuality, 0, names.Length - 1);
        UpdateQualityLabel();
    }

    private void ResetSettings()
    {
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.DeleteKey(SfxVolumeKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(QualityKey);

        PlayerPrefs.Save();
        ApplySavedSettings();
        RebuildPanel();
    }

    private void ChangeQuality(int direction)
    {
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            return;

        qualityIndex = Mathf.Clamp(qualityIndex + direction, 0, names.Length - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.Save();
        UpdateQualityLabel();
    }

    private void UpdateQualityLabel()
    {
        if (qualityValueLabel == null)
            return;

        string[] names = QualitySettings.names;
        qualityValueLabel.text = names == null || names.Length == 0
            ? "Default"
            : names[Mathf.Clamp(qualityIndex, 0, names.Length - 1)];
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
        if (audioMixerOverride != null)
            return audioMixerOverride;

        AudioSource[] audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            if (source != null && source.outputAudioMixerGroup != null)
                return source.outputAudioMixerGroup.audioMixer;
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
}
