using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private UnitPanelTexturesSO _texturesSO;
    
    private ViewController _UIController;

    private readonly Dictionary<UnitClass, UnitProfile> _unitProfilesDict = new();

    private VisualElement _bottomSection;


    private void BuildUnitPanel()
    {
        EventMediator abilityEventListener = EventMediator.Instance;
        VisualElement root = _uiDocument.rootVisualElement;

        root.Clear();
        root.styleSheets.Clear();

        _UIController = ViewController.Instance;
        foreach (StyleSheet sheet in _UIController.DefaultStyleSheet.BaseStyles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in _styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        _bottomSection = UITK.AddElement(root, "P2", "bottomSection");

        abilityEventListener.OnUnitSpawned += BuildUnitProfile;
    }

    private void BuildUnitProfile(UnitClass unitClass, SoldierAttributesConfig soldierConfig)
    {
        var newUnitProfile = new UnitProfile(unitClass, soldierConfig, _bottomSection, _texturesSO, _UIController);

        _unitProfilesDict.Add(unitClass, newUnitProfile);

        EventMediator.Instance.OnDamageReceived += newUnitProfile.OnHealthChanged;
        EventMediator.Instance.OnAbilityStarted += newUnitProfile.OnAbilityStarted;
        EventMediator.Instance.OnAbilityEnded += newUnitProfile.OnAbilityEnded;
        EventMediator.Instance.OnCooldownEnded += newUnitProfile.OnCooldownEnded;

        newUnitProfile.unitProfile.RegisterCallback<ClickEvent>(_ =>
            UnitSelectionManager.Instance.SelectUnit(unitClass));

        newUnitProfile.SkillButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        int index = _unitProfilesDict.Count - 1;
        newUnitProfile.SkillButton.clicked += () =>
        Presenter.Instance.InvokeAbilityPress(index);
    }


    private class UnitProfile
    {
        // Each UnitProfile owns the full UI block for a single unit class and its related event bindings.
        public readonly UnitClass UnitClass;
        public readonly SoldierAttributesConfig SoldierConfig;
        public readonly BodyConfig BodyConfig;
        public readonly WeaponConfig WeaponConfig;
        public readonly SkillConfig SkillConfig;

        public readonly VisualElement unitProfile;
        public readonly VisualElement AttributesContainer;

        public readonly Button SkillButton;
        public readonly ProgressBar CooldownBar;
        public readonly ProgressBar HealthBar;


        public UnitProfile(UnitClass unitClass, SoldierAttributesConfig soldierConfig,
            VisualElement bottomSection, UnitPanelTexturesSO textures, ViewController UIController)
        {
            this.UnitClass = unitClass;
            this.SoldierConfig = soldierConfig;
            BodyConfig = soldierConfig.bodyConfig;
            WeaponConfig = soldierConfig.weaponConfigs[0];
            SkillConfig = soldierConfig.skillConfigs[0];

            unitProfile = UITK.AddElement(bottomSection, "unitProfile");
            BuildModuleMesh(UIController);

            AttributesContainer = UITK.AddElement(unitProfile, "attributesContainer");
            BuildSkillIcon(soldierConfig, out SkillButton, out CooldownBar);
            BuildHPStatsBoard(textures, out HealthBar);
        }

        private void BuildModuleMesh(ViewController UIController)
        {
            var moduleMesh = UITK.AddElement(unitProfile, "moduleMesh");
            moduleMesh.style.backgroundImage = UIController.BaseTextures.LinearGradient;
        }

        private void BuildSkillIcon(SoldierAttributesConfig soldierConfig, out Button skillButton, out ProgressBar cooldownBar)
        {
            var skillBlock = UITK.AddElement(AttributesContainer, "skillBlock");

            skillButton = UITK.AddElement<Button>(skillBlock, "RigidButton", "skillButton");
            skillButton.style.backgroundImage = soldierConfig.icon;
            skillButton.AddToClassList("Activated");
            skillButton.EnableInClassList("Activated", false);
            skillButton.AddToClassList("Cooldown");
            skillButton.EnableInClassList("Cooldown", false);

            cooldownBar = UITK.AddElement<ProgressBar>(skillBlock, "skillCooldownBar");
            cooldownBar.highValue = SkillConfig.cooldown;
            cooldownBar.value = SkillConfig.cooldown;
        }

        private void BuildHPStatsBoard(UnitPanelTexturesSO icons, out ProgressBar healthBar)
        {
            var healthStatsBoard = UITK.AddElement(AttributesContainer, "healthStatsBoard");

            healthBar = UITK.AddElement<ProgressBar>(healthStatsBoard, "healthBar");
            healthBar.highValue = BodyConfig.maxHealth;
            healthBar.value = BodyConfig.maxHealth;

            //TO-DO link with in-gmae stats
            var statsBoard = UITK.AddElement(healthStatsBoard, "P1", "statsBoard");
            //statsBoard.style.backgroundImage = icons.StatsBoardBG;

            //var weaponRow = UITK.AddElement(statsBoard, "statsRow");
            //var DMG = UITK.AddElement<Label>(weaponRow, "DMG", "statElement");
            //DMG.text = "x 1.5";
            //var FIRE = UITK.AddElement<Label>(weaponRow, "FIRE", "statElement");
            //FIRE.text = "x 1.5";
            //var DST = UITK.AddElement<Label>(weaponRow, "DST", "statElement");
            //DST.text = "x 1.5";

            //var skillRow = UITK.AddElement(statsBoard, "statsRow");
            //var PWR = UITK.AddElement<Label>(skillRow, "PWR", "statElement");
            //PWR.text = "x 1.5";
            //var TIME = UITK.AddElement<Label>(skillRow, "TIME", "statElement");
            //TIME.text = "x 1.5";
            //var AREA = UITK.AddElement<Label>(skillRow, "AREA", "statElement");
            //AREA.text = "x 1.5";
        }

        public void OnHealthChanged(DamageEvent evt)
        {
            // The event is broadcasted globally, so each profile filters for its own unit and updates only its cached bar.
            if (evt.TargetEntityClass!= this.UnitClass) return;

            HealthBar.value = Mathf.Clamp(HealthBar.value - evt.DamageAmount, 0f, HealthBar.highValue);
        }

        public void OnAbilityStarted(AbilityStartedEvent evt)
        {
            if (evt.Type != SkillConfig.type) return;

            SkillButton.EnableInClassList("Activated", true);
            CooldownBar.value = 0;
        }

        public async void OnAbilityEnded(AbilityEndedEvent evt)
        {
            if (evt.Type != SkillConfig.type) return;
            if (evt.Cooldown <= 0f) return;

            SkillButton.EnableInClassList("Activated", false);
            SkillButton.EnableInClassList("Cooldown", true);

            CooldownBar.highValue = evt.Cooldown;

            Awaitable timer = Awaitable.WaitForSecondsAsync(evt.Cooldown);
            while (!timer.IsCompleted)
            {
                CooldownBar.value += Time.deltaTime;
                await Awaitable.NextFrameAsync();
            }

            CooldownBar.value = evt.Cooldown;
        }

        public void OnCooldownEnded(AbilityCooldownEndedEvent evt)
        {
            if (evt.Type != SkillConfig.type) return;
            
            SkillButton.EnableInClassList("Activated", false);
            SkillButton.EnableInClassList("Cooldown", false);
        }
    }


    private void Awake()
    {
        BuildUnitPanel();
    }
}
