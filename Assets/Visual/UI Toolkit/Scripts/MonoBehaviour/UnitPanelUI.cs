using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private UnitPanelTexturesSO texturesSO;

    private VisualElement unitPanel;
    private Dictionary<UnitClass, UnitProfile> unitProfilesDict = new();

    public ClassConfig classConfigTester;
    public WeaponConfig weaponConfigTester;
    public SkillConfig skillConfigTester;


    private void InitializeUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in UIControllerManager.Instance.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        unitPanel = UITK.AddElement(root, "unitPanel", "P");

        AbilityEventListener.Instance.OnUnitSpawned += UnitProfileInitHandler;

        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Raider, classConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Sniper, classConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Juggernaut, classConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Arsonist, classConfigTester, weaponConfigTester, skillConfigTester);
    }

    private void UnitProfileInitHandler(UnitClass unitClass, ClassConfig classConfig, WeaponConfig weaponConfig, SkillConfig skillConfig)
    {
        var newUnitProfile = new UnitProfile(unitClass, classConfig, weaponConfig, skillConfig,
            unitPanel, texturesSO);

        unitProfilesDict.Add(unitClass, newUnitProfile);
    }


    private class UnitProfile : VisualElement
    {
        public UnitClass unitClass;
        public ClassConfig classConfig;
        public WeaponConfig weaponConfig;
        public SkillConfig skillConfig;

        public VisualElement unitProfile;

        public bool isInfoPanelOpen = false;


        public UnitProfile(UnitClass unitClass, ClassConfig classConfig, WeaponConfig weaponConfig, SkillConfig skillConfig,
            VisualElement unitPanel, UnitPanelTexturesSO icons)
        {
            this.unitClass = unitClass;
            this.classConfig = classConfig;
            this.weaponConfig = weaponConfig;
            this.skillConfig = skillConfig;

            InitializeGeneralProfile(unitPanel, UIControllerManager.Instance.colorScheme, icons);
        }

        private void InitializeGeneralProfile(VisualElement unitPanel, ColorSchemeSO colorScheme, UnitPanelTexturesSO icons)
        {
            unitProfile = UITK.AddElement(unitPanel, "unitProfile");
            unitProfile.style.backgroundColor = colorScheme.Black;

            var statsBoard = UITK.AddElement(unitProfile, "statsBoard", "H2");
            statsBoard.style.backgroundColor = colorScheme.gray;
            statsBoard.style.backgroundImage = icons.statsBoardBG;

                var weaponBoard = UITK.AddElement(statsBoard, "statBoard");
                weaponBoard.style.color = colorScheme.white;

                    var DMG = UITK.AddElement<Label>(weaponBoard, "DMG", "statElement");
                    DMG.text = weaponConfig.damage.ToString();

                    var FIRE = UITK.AddElement<Label>(weaponBoard, "FIRE", "statElement");
                    FIRE.text = weaponConfig.fireRate.ToString();

                    var DST = UITK.AddElement<Label>(weaponBoard, "DST", "statElement");
                    DST.text = weaponConfig.range.ToString();

                var skillBoard = UITK.AddElement(statsBoard, "statBoard");
                skillBoard.style.color = colorScheme.white;

                    var PWR = UITK.AddElement<Label>(skillBoard, "PWR", "statElement");
                    PWR.text = skillConfig.power.ToString();

                    var TIME = UITK.AddElement<Label>(skillBoard, "TIME", "statElement");
                    TIME.text = skillConfig.duration.ToString() + "/"
                                + skillConfig.cooldown.ToString();

                    var AREA = UITK.AddElement<Label>(skillBoard, "AREA", "statElement");
                    AREA.text = skillConfig.area.ToString() + "/"
                                + skillConfig.range.ToString();


            var skillHealthBlock = UITK.AddElement(unitProfile, "skillHealthBlock");

                var skillButton = UITK.AddElement<Button>(skillHealthBlock, "skillButton");
                skillButton.style.backgroundImage = icons.classAbilityIcons[0];

                var healthBar = UITK.AddElement<ProgressBar>(skillHealthBlock, "healthBar");
                healthBar.highValue = 100;
                healthBar.value = 100;


            var infoPanel = UITK.AddElement(unitProfile, "infoPanel");
            infoPanel.style.backgroundColor = colorScheme.darkGray;

                var infoPanelButton = UITK.AddElement<Button>(infoPanel, "infoPanelButton", "RigidButton");
                infoPanelButton.style.backgroundColor = colorScheme.lightGray;

                infoPanel.Add(classConfig.SetInfoBlockUI(infoPanel, colorScheme));
                infoPanel.Add(weaponConfig.SetInfoBlockUI(infoPanel, colorScheme));
                infoPanel.Add(skillConfig.SetInfoBlockUI(infoPanel, colorScheme));

            infoPanelButton.clicked += () =>
            {
                if (isInfoPanelOpen)
                {
                    unitProfile.style.top = 0;
                    infoPanel.style.height = 20;
                    isInfoPanelOpen = false;
                }
                else
                {
                    unitProfile.style.top = -40;
                    infoPanel.style.height = 260;
                    isInfoPanelOpen = true;
                }
            };

            AbilityEventListener.Instance.OnHealthChanged += (unitClass, health) =>
            {
                if (unitClass != this.unitClass) return;

                healthBar.value = health;
            };
        }
    }


    private void Awake()
    {
        InitializeUI();
    }
}
