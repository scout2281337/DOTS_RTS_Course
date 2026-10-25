using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private UnitPanelTexturesSO _texturesSO;
    [SerializeField] private ModuleBaseSO testModule;

    private ViewController _UIController;
    private EventMediator _eventMediator;

    private Dictionary<UnitClass, UnitProfile> _unitProfilesDict = new();

    private VisualElement _bottomSection;


    private void BuildUnitPanel()
    {
        _eventMediator = EventMediator.Instance;
        VisualElement root = _uiDocument.rootVisualElement;

        ClearUnitProfiles();

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

        if (_eventMediator != null)
        {
            _eventMediator.OnUnitSpawned -= BuildUnitProfile;
            _eventMediator.OnUnitSpawned += BuildUnitProfile;
        }
    }

    private void BuildUnitProfile(UnitClass unitClass, SoldierAttributesConfig soldierConfig)
    {
        if (_bottomSection == null || soldierConfig == null)
            return;

        if (_unitProfilesDict.TryGetValue(unitClass, out UnitProfile oldUnitProfile))
        {
            UnbindUnitProfile(oldUnitProfile);
            oldUnitProfile.unitProfile.RemoveFromHierarchy();
            _unitProfilesDict.Remove(unitClass);
        }

        var newUnitProfile = new UnitProfile(unitClass, soldierConfig, _bottomSection, _texturesSO, _UIController);

        _unitProfilesDict[unitClass] = newUnitProfile;

        if (_eventMediator != null)
        {
            _eventMediator.OnDamageReceived += newUnitProfile.OnHealthChanged;
            _eventMediator.OnAbilityStarted += newUnitProfile.OnAbilityStarted;
            _eventMediator.OnAbilityEnded += newUnitProfile.OnAbilityEnded;
            _eventMediator.OnCooldownEnded += newUnitProfile.OnCooldownEnded;
            _eventMediator.OnNewModule += newUnitProfile.OnNewModule;
        }

        newUnitProfile.unitProfile.RegisterCallback<ClickEvent>(_ =>
            UnitSelectionManager.Instance.SelectUnit(unitClass));

        newUnitProfile.SkillButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        int index = GetAbilitySlotIndex(unitClass);
        newUnitProfile.SkillButton.clicked += () =>
        Presenter.Instance.InvokeAbilityPress(index);
    }

    private void ClearUnitProfiles()
    {
        foreach (UnitProfile unitProfile in _unitProfilesDict.Values)
        {
            UnbindUnitProfile(unitProfile);
            unitProfile.unitProfile.RemoveFromHierarchy();
        }

        _unitProfilesDict.Clear();
    }

    private void UnbindUnitProfile(UnitProfile unitProfile)
    {
        if (_eventMediator == null || unitProfile == null)
            return;

        _eventMediator.OnDamageReceived -= unitProfile.OnHealthChanged;
        _eventMediator.OnAbilityStarted -= unitProfile.OnAbilityStarted;
        _eventMediator.OnAbilityEnded -= unitProfile.OnAbilityEnded;
        _eventMediator.OnCooldownEnded -= unitProfile.OnCooldownEnded;
    }

    private static int GetAbilitySlotIndex(UnitClass unitClass)
    {
        return unitClass switch
        {
            UnitClass.Raider => 0,
            UnitClass.Juggernaut => 1,
            UnitClass.Arsonist => 2,
            UnitClass.Sniper => 3,
            _ => 0
        };
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

        public readonly VisualElement moduleMesh;
        public int moduleAmount = 0;

        public readonly Button SkillButton;
        public readonly ProgressBar CooldownBar;

        public readonly VisualElement healthBar;
        public readonly ProgressBar delayedHealthBar;
        public readonly ProgressBar trueHealthBar;
        public readonly Label healthAmount;


        public UnitProfile(UnitClass unitClass, SoldierAttributesConfig soldierConfig,
            VisualElement bottomSection, UnitPanelTexturesSO textures, ViewController UIController)
        {
            this.UnitClass = unitClass;
            this.SoldierConfig = soldierConfig;
            BodyConfig = soldierConfig.bodyConfig;
            WeaponConfig = soldierConfig.weaponConfigs[0];
            SkillConfig = soldierConfig.skillConfigs[0];

            unitProfile = UITK.AddElement(bottomSection, "unitProfile");
            BuildModuleMesh(UIController, out moduleMesh);

            AttributesContainer = UITK.AddElement(unitProfile, "attributesContainer");
            BuildSkillIcon(soldierConfig, out SkillButton, out CooldownBar);
            BuildHealthBar(textures, out healthBar, out delayedHealthBar, out trueHealthBar, out healthAmount);
        }

        private void BuildModuleMesh(ViewController UIController, out VisualElement moduleMesh)
        {
            moduleMesh = UITK.AddElement(unitProfile, "moduleMesh");
            moduleMesh.style.backgroundImage = UIController.BaseTextures.LinearGradient;
        }

        private void BuildSkillIcon(SoldierAttributesConfig soldierConfig,
            out Button skillButton, out ProgressBar cooldownBar)
        {
            var skillBlock = UITK.AddElement(AttributesContainer, "skillBlock");

            var cooldownBarContainer = UITK.AddElement(skillBlock, "cooldownBarContainer"); // Because we are rotating the progress bar we need a separate container so that we can keep correct layout

            cooldownBar = UITK.AddElement<ProgressBar>(cooldownBarContainer, "skillCooldownBar");
            cooldownBar.highValue = SkillConfig.cooldown;
            cooldownBar.value = SkillConfig.cooldown;

            skillButton = UITK.AddElement<Button>(skillBlock, "RigidButton", "skillButton");
            skillButton.style.backgroundImage = soldierConfig.icon;
            skillButton.AddToClassList("Activated");
            skillButton.EnableInClassList("Activated", false);
            skillButton.AddToClassList("Cooldown");
            skillButton.EnableInClassList("Cooldown", false);
        }

        private void BuildHealthBar(UnitPanelTexturesSO icons, out VisualElement healthBar, 
            out ProgressBar delayedHealthBar, out ProgressBar trueHealthBar, out Label healthAmount)
        {

            healthBar = UITK.AddElement(AttributesContainer, "P1", "healthBar");

            delayedHealthBar = UITK.AddElement<ProgressBar>(healthBar, "delayedHealthBar");
            delayedHealthBar.highValue = BodyConfig.maxHealth;
            delayedHealthBar.value = BodyConfig.maxHealth;

            trueHealthBar = UITK.AddElement<ProgressBar>(healthBar, "trueHealthBar");
            trueHealthBar.highValue = BodyConfig.maxHealth;
            trueHealthBar.value = BodyConfig.maxHealth;

            healthAmount = UITK.AddElement<Label>(healthBar, "H2", "healthAmount");
            healthAmount.text = BodyConfig.maxHealth.ToString();

        }

        public async void OnHealthChanged(DamageEvent evt)
        {
            // The event is broadcasted globally, so each profile filters for its own unit.
            if (evt.TargetEntityClass!= this.UnitClass) return;

            float newHealthAmount = Mathf.Clamp(trueHealthBar.value - evt.DamageAmount, 0f, trueHealthBar.highValue);
            trueHealthBar.value = newHealthAmount;
            healthAmount.text = Mathf.RoundToInt(newHealthAmount).ToString();

            //Animation: Twitch
            Vector2 twitchDirection = new Vector2(Random.value - 0.5f, Random.value - 0.5f).normalized; // - 0.5 to centralize and allow negative 
            float power = evt.DamageAmount;

            // Way back
            float timer = 0;
            float duration = 0.3f;
            while (timer  < duration)
            {
                timer += Time.deltaTime;
                float normalizedTimer = timer / duration; // Normalized to go from 0 to 1 despite actual timer

                healthBar.style.left = Mathf.Lerp(twitchDirection.x * power, 0, Easing.OutCubic(normalizedTimer));
                healthBar.style.top = Mathf.Lerp(twitchDirection.y * power, 0, Easing.OutCubic(normalizedTimer));

                await Awaitable.NextFrameAsync();
            }

            //Animation: DelayedHealthBar
            timer = 0;
            duration = 0.2f;
            float previousValue = delayedHealthBar.value;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float normalizedTimer = timer / duration; // Normalized to go from 0 to 1 despite actual timer

                delayedHealthBar.value = Mathf.Lerp(previousValue, newHealthAmount, Easing.OutCubic(normalizedTimer));

                await Awaitable.NextFrameAsync();
            }
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

        public void OnNewModule(UnitClass unit, ModuleBaseSO moduleSO)
        {
            // The event is broadcasted globally, so each profile filters for its own unit.
            if (unit != this.UnitClass) return;

            moduleAmount++;

            string moduleCat = moduleSO.category switch
            {
                ModuleCategory.WeaponPower => "WP",
                ModuleCategory.TacticalSystem => "TS",
                ModuleCategory.DefensiveProtocol => "DP",
                _ => "Error"
            };

            var newModule = UITK.AddElement(moduleMesh, moduleCat, "moduleSlot");
            int shiftDir = moduleAmount % 2 == 0 ? 1 : -1;
            newModule.style.top = 10 * shiftDir;

            var newModuleIcon = UITK.AddElement(newModule, "moduleIcon");
            newModuleIcon.style.backgroundImage = moduleSO.wideIcon;
        }
    }


    private void Awake()
    {
        BuildUnitPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            _unitProfilesDict[UnitClass.Arsonist].OnNewModule(UnitClass.Arsonist, testModule);
        }
    }

    private void OnDestroy()
    {
        if (_eventMediator != null)
            _eventMediator.OnUnitSpawned -= BuildUnitProfile;

        ClearUnitProfiles();
    }
}
