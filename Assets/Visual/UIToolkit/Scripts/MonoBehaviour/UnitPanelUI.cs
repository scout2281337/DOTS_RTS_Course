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

    public BodyConfig bodyConfigTester;
    public WeaponConfig weaponConfigTester;
    public SkillConfig skillConfigTester;


    private void BuildUI()
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

        unitPanel = UITK.AddElement(root, "P2", "unitPanel");

        AbilityEventListener.Instance.OnUnitSpawned += UnitProfileInitHandler;

        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Raider, bodyConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Sniper, bodyConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Juggernaut, bodyConfigTester, weaponConfigTester, skillConfigTester);
        AbilityEventListener.Instance.InvokeUnitSpawned(UnitClass.Arsonist, bodyConfigTester, weaponConfigTester, skillConfigTester);
    }

    private void UnitProfileInitHandler(UnitClass unitClass, BodyConfig bodyConfig, WeaponConfig weaponConfig, SkillConfig skillConfig)
    {
        var newUnitProfile = new UnitProfile(unitClass, bodyConfig, weaponConfig, skillConfig,
            unitPanel, texturesSO);

        unitProfilesDict.Add(unitClass, newUnitProfile);
    }


    private class UnitProfile : VisualElement
    {
        public UnitClass unitClass;
        public BodyConfig bodyConfig;
        public WeaponConfig weaponConfig;
        public SkillConfig skillConfig;

        public VisualElement unitProfile;

        public bool isInfoPanelOpen = false;


        public UnitProfile(UnitClass unitClass, BodyConfig bodyConfig, WeaponConfig weaponConfig, SkillConfig skillConfig,
            VisualElement unitPanel, UnitPanelTexturesSO icons)
        {
            this.unitClass = unitClass;
            this.bodyConfig = bodyConfig;
            this.weaponConfig = weaponConfig;
            this.skillConfig = skillConfig;

            BuildGeneralProfile(unitPanel, icons);
        }

        private void BuildGeneralProfile(VisualElement unitPanel, UnitPanelTexturesSO icons)
        {
            unitProfile = UITK.AddElement(unitPanel, "unitProfile");

            var statsBoard = UITK.AddElement(unitProfile, "P1", "statsBoard");
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


            var skillHealthBlock = UITK.AddElement(unitProfile, "skillHealthBlock");

                var skillButton = UITK.AddElement<Button>(skillHealthBlock, "skillButton");
                skillButton.style.backgroundImage = icons.classAbilityIcons[0];

                var healthBar = UITK.AddElement<ProgressBar>(skillHealthBlock, "healthBar");
                healthBar.highValue = 100;
                healthBar.value = 100;


            var infoPanel = UITK.AddElement(unitProfile, "infoPanel");

                var infoPanelButton = UITK.AddElement<Button>(infoPanel, "infoPanelButton", "RigidButton");

                infoPanel.Add(bodyConfig.SetInfoBlockUI(infoPanel));
                infoPanel.Add(weaponConfig.SetInfoBlockUI(infoPanel));
                infoPanel.Add(skillConfig.SetInfoBlockUI(infoPanel));

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
        BuildUI();
    }
}
