using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ViewController UIController;
    [SerializeField] private UnitPanelTexturesSO texturesSO;

    private readonly Dictionary<UnitClass, UnitProfile> unitProfilesDict = new();

    private VisualElement bottomSection;

    [Header("Testing")]
    public BodyConfig bodyConfigTester;
    public WeaponConfig weaponConfigTester;
    public SkillConfig skillConfigTester;

    private static readonly UnitClass[] PreviewUnits =
    {
        UnitClass.Raider,
        UnitClass.Sniper,
        UnitClass.Juggernaut,
        UnitClass.Arsonist
    };

    private void BuildUnitPanel()
    {
        AbilityEventListener abilityEventListener = AbilityEventListener.Instance;
        VisualElement root = uiDocument.rootVisualElement;

        // The panel is rebuilt from code, so we start from a clean visual tree and re-attach styles explicitly.
        root.Clear();
        root.styleSheets.Clear();

        foreach (StyleSheet sheet in UIController.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        bottomSection = UITK.AddElement(root, "P2", "bottomSection");

        abilityEventListener.OnUnitSpawned += BuildUnitProfile;

        // Temporary preview data so the panel can be inspected without waiting for gameplay spawning.
        foreach (UnitClass previewUnit in PreviewUnits)
        {
            abilityEventListener.InvokeUnitSpawned(previewUnit, bodyConfigTester, weaponConfigTester, skillConfigTester);
        }
    }

    private void BuildUnitProfile(UnitClass unitClass, BodyConfig bodyConfig, WeaponConfig weaponConfig, SkillConfig skillConfig)
    {
        // Replacing the existing entry keeps the dictionary aligned with the current visual state for that unit class.
        if (unitProfilesDict.TryGetValue(unitClass, out UnitProfile existingProfile))
            DisposeUnitProfile(existingProfile);
        
        var newUnitProfile = new UnitProfile(unitClass, bodyConfig, weaponConfig, skillConfig,
            bottomSection, texturesSO, UIController);

        unitProfilesDict[unitClass] = newUnitProfile;

        AbilityEventListener.Instance.OnHealthChanged += newUnitProfile.OnHealthChanged;
    }

    private void DisposeUnitProfile(UnitProfile unitProfile)
    {
        // Cleanup has to remove both the visual element and the event subscription, otherwise old profiles keep receiving updates.
        AbilityEventListener.Instance.OnHealthChanged -= unitProfile.OnHealthChanged;
        unitProfile.unitProfile.RemoveFromHierarchy();
        unitProfilesDict.Remove(unitProfile.unitClass);
    }


    private class UnitProfile
    {
        // Each UnitProfile owns the full UI block for a single unit class and its related event bindings.
        public readonly UnitClass unitClass;
        public readonly BodyConfig bodyConfig;
        public readonly WeaponConfig weaponConfig;
        public readonly SkillConfig skillConfig;

        public readonly VisualElement unitProfile;
        public readonly VisualElement attributesContainer;

        public readonly ProgressBar healthBar;


        public UnitProfile(UnitClass unitClass, BodyConfig bodyConfig, WeaponConfig weaponConfig, SkillConfig skillConfig,
            VisualElement bottomSection, UnitPanelTexturesSO textures, ViewController UIController)
        {
            this.unitClass = unitClass;
            this.bodyConfig = bodyConfig;
            this.weaponConfig = weaponConfig;
            this.skillConfig = skillConfig;

            this.unitProfile = UITK.AddElement(bottomSection, "unitProfile");
            BuildModuleMesh(UIController);

            this.attributesContainer = UITK.AddElement(unitProfile, "attributesContainer");
            healthBar = BuildSkillHPBar(textures);
            BuildStatsBoard(textures);
        }

        private void BuildModuleMesh(ViewController UIController)
        {
            var moduleMesh = UITK.AddElement(unitProfile, "moduleMesh");
            moduleMesh.style.backgroundImage = UIController.baseTextures.linearGradient;
        }

        private ProgressBar BuildSkillHPBar(UnitPanelTexturesSO icons)
        {
            var skillHealthBlock = UITK.AddElement(attributesContainer, "skillHealthBlock");

            var skillButton = UITK.AddElement<Button>(skillHealthBlock, "skillButton");
            skillButton.style.backgroundImage = icons.classAbilityIcons[0];

            var healthBar = UITK.AddElement<ProgressBar>(skillHealthBlock, "healthBar");
            healthBar.highValue = bodyConfig.maxHealth;
            healthBar.value = bodyConfig.maxHealth;

            return healthBar;
        }

        private void BuildStatsBoard(UnitPanelTexturesSO icons)
        {
            var statsBoard = UITK.AddElement(attributesContainer, "P1", "statsBoard");
            statsBoard.style.backgroundImage = icons.statsBoardBG;

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
