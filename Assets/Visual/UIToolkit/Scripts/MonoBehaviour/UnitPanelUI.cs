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
        AbilityEventListener abilityEventListener = AbilityEventListener.Instance;
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

        AbilityEventListener.Instance.OnHealthChanged += newUnitProfile.OnHealthChanged;

        int index = _unitProfilesDict.Count - 1;
        newUnitProfile.skillButton.clicked += () =>
        Presenter.Instance.InvokeAbilityPress(index);
    }


    private class UnitProfile
    {
        // Each UnitProfile owns the full UI block for a single unit class and its related event bindings.
        public readonly UnitClass unitClass;
        public readonly SoldierAttributesConfig soldierConfig;
        public readonly BodyConfig bodyConfig;
        public readonly WeaponConfig weaponConfig;
        public readonly SkillConfig skillConfig;

        public readonly VisualElement unitProfile;
        public readonly VisualElement attributesContainer;

        public readonly Button skillButton;
        public readonly ProgressBar healthBar;


         public UnitProfile(UnitClass unitClass, SoldierAttributesConfig soldierConfig,
            VisualElement bottomSection, UnitPanelTexturesSO textures, ViewController UIController)
        {
            this.unitClass = unitClass;
            this.soldierConfig = soldierConfig;
            bodyConfig = soldierConfig.bodyConfig;
            weaponConfig = soldierConfig.weaponConfigs[0];
            skillConfig = soldierConfig.skillConfigs[0];

            unitProfile = UITK.AddElement(bottomSection, "unitProfile");
            BuildModuleMesh(UIController);

            attributesContainer = UITK.AddElement(unitProfile, "attributesContainer");
            BuildSkillHPBar(soldierConfig, out skillButton, out healthBar);
            BuildStatsBoard(textures);
        }

        private void BuildModuleMesh(ViewController UIController)
        {
            var moduleMesh = UITK.AddElement(unitProfile, "moduleMesh");
            moduleMesh.style.backgroundImage = UIController.BaseTextures.LinearGradient;
        }

        private void BuildSkillHPBar(SoldierAttributesConfig soldierConfig, out Button skillButton, out ProgressBar healthBar)
        {
            var skillHealthBlock = UITK.AddElement(attributesContainer, "skillHealthBlock");

            skillButton = UITK.AddElement<Button>(skillHealthBlock, "skillButton");
            skillButton.style.backgroundImage = soldierConfig.icon;

            healthBar = UITK.AddElement<ProgressBar>(skillHealthBlock, "healthBar");
            healthBar.highValue = bodyConfig.maxHealth;
            healthBar.value = bodyConfig.maxHealth;
        }

        private void BuildStatsBoard(UnitPanelTexturesSO icons)
        {
            var statsBoard = UITK.AddElement(attributesContainer, "P1", "statsBoard");
            statsBoard.style.backgroundImage = icons.StatsBoardBG;

            var weaponBoard = UITK.AddElement(statsBoard, "statBoard");

            var DMG = UITK.AddElement<Label>(weaponBoard, "DMG", "statElement");
            DMG.text = weaponConfig.damage.ToString();

            var FIRE = UITK.AddElement<Label>(weaponBoard, "FIRE", "statElement");
            FIRE.text = weaponConfig.fireRate.ToString();

            var DST = UITK.AddElement<Label>(weaponBoard, "DST", "statElement");
            DST.text = weaponConfig.range.ToString();

            var skillBoard = UITK.AddElement(statsBoard, "statBoard");

            var PWR = UITK.AddElement<Label>(skillBoard, "PWR", "statElement");
            PWR.text = skillConfig.power.ToString();

            var TIME = UITK.AddElement<Label>(skillBoard, "TIME", "statElement");
            TIME.text = skillConfig.duration.ToString() + "/"
                        + skillConfig.cooldown.ToString();

            var AREA = UITK.AddElement<Label>(skillBoard, "AREA", "statElement");
            AREA.text = skillConfig.area.ToString() + "/"
                        + skillConfig.range.ToString();
        }

        public void OnHealthChanged(UnitClass unitClass, float healthDelta)
        {
            // The event is broadcasted globally, so each profile filters for its own unit and updates only its cached bar.
            if (unitClass != this.unitClass) return;

            healthBar.value = Mathf.Clamp(healthBar.value - healthDelta, 0f, healthBar.highValue);
        }
    }


    private void Awake()
    {
        BuildUnitPanel();
    }
}
