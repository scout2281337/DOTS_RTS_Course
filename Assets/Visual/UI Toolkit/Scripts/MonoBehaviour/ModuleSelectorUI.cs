using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModuleSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ModuleSelectorTexturesSO texturesSO;

    [SerializeField] private int modulesPerWave = 3;
    [SerializeField] private int modulationTokens = 3;

    private bool isUpgradable = true;
    private bool isHidden = false;
    private bool isStabilized = false;
    private int takenModules = 0;
    private Dictionary<UnitClass, VisualElement> unitAssignedBox = new();
    private Dictionary<UnitClass, Module> unitDisplayedModules = new();

    private VisualElement moduleSelectorScreen;
    private VisualElement moduleScreenBG;
    private VisualElement moduleBoard;
    private VisualElement[] moduleBoxes;
    private Button upgradeButton;
    private Button recalibrateButton;
    private Button stabilizeButton;
    private Label tokenModuleTracker;


    private void InitializeUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        var UICtrlMng = UIControllerManager.Instance;

        foreach (StyleSheet sheet in UICtrlMng.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        moduleSelectorScreen = UITK.AddElement(root, "moduleSelectorScreen", "P");

        moduleScreenBG = UITK.AddElement(moduleSelectorScreen, "moduleScreenBG");

        moduleBoard = UITK.AddElement(moduleSelectorScreen, "moduleBoard");

        upgradeButton = UITK.AddElement<Button>(moduleSelectorScreen, "H1", "upgradeButton");
        upgradeButton.style.backgroundColor = UICtrlMng.colorScheme.accentCyan;
        upgradeButton.style.color = UICtrlMng.colorScheme.white;
        upgradeButton.text = "UPGRADE";
        upgradeButton.clicked += StartUpgrade;

        recalibrateButton = UITK.AddElement<Button>(moduleSelectorScreen, "H1", "recalibrateButton");
        recalibrateButton.style.backgroundColor = UICtrlMng.colorScheme.darkGray;
        recalibrateButton.style.color = UICtrlMng.colorScheme.white;
        recalibrateButton.text = "RECALIBRATE";
        recalibrateButton.clicked += RecalibrateModules;

        stabilizeButton = UITK.AddElement<Button>(moduleSelectorScreen, "H1", "stabilizeButton");
        stabilizeButton.style.backgroundColor = UICtrlMng.colorScheme.darkGray;
        stabilizeButton.style.color = UICtrlMng.colorScheme.white;
        stabilizeButton.text = "STABILIZE";
        stabilizeButton.clicked += StabilizeModules;

        tokenModuleTracker = UITK.AddElement<Label>(moduleSelectorScreen, "H1", "tokenModuleTracker");
        tokenModuleTracker.style.backgroundColor = UICtrlMng.colorScheme.darkGray;
        tokenModuleTracker.style.color = UICtrlMng.colorScheme.white;
        tokenModuleTracker.text = "ML: " + modulesPerWave + "   MT: " + modulationTokens;

        HideModuleBoard();
        UnlockUpgrade();
    }

    private void StartUpgrade()
    {
        RevealModuleBoard();
        HideUpgradeButton();

        GenerateModules();
    }

    private void RecalibrateModules()
    {
        if (modulationTokens <= 0) return;
        if (isStabilized) return;

        GenerateModules();
        modulationTokens--;

        UpdateModuleScreen();
    }

    private void StabilizeModules()
    {
        if (modulationTokens <= 0) return;
        if (isStabilized) return;

        isStabilized = true;
        modulationTokens--;

        UpdateModuleScreen();
    }

    private void HideModuleBoard()
    {
        moduleBoard.style.display = DisplayStyle.None;
        recalibrateButton.style.display = DisplayStyle.None;
        stabilizeButton.style.display = DisplayStyle.None;
        tokenModuleTracker.style.display = DisplayStyle.None;
        moduleScreenBG.style.display = DisplayStyle.None;

        isHidden = true;
    }

    private void RevealModuleBoard()
    {
        moduleBoard.style.display = DisplayStyle.Flex;
        recalibrateButton.style.display = DisplayStyle.Flex;
        stabilizeButton.style.display = DisplayStyle.Flex;
        tokenModuleTracker.style.display = DisplayStyle.Flex;
        moduleScreenBG.style.display = DisplayStyle.Flex;

        UpdateModuleScreen();

        isHidden = false;
    }

    private void UnlockUpgrade()
    {
        upgradeButton.style.display = DisplayStyle.Flex;
        isUpgradable = true;
    }

    private void HideUpgradeButton()
    {
        upgradeButton.style.display = DisplayStyle.None;
        isUpgradable = false;
    }

    private void GenerateModules()
    {
        if (isStabilized)
        {
            isStabilized = false;
            return;
        }

        var unitDict = FriendlyUnitManager.Instance.unitEntityDict;
        var modGen = ModuleManager.Instance;

        // First generation
        if (unitAssignedBox.Count == 0)
        {
            moduleBoxes = new VisualElement[unitDict.Count];

            int i = 0;
            foreach (var unit in unitDict.Keys)
            {
                var box = UITK.AddElement(moduleBoard, "moduleBox");

                moduleBoxes[i++] = box;
                unitAssignedBox.Add(unit, box);

                ModuleInitHandler(unit, modGen.GetRandomModuleForUnit(unit));
            }
        }
        // Recalibration
        else
        {
            foreach (var unit in unitDict.Keys)
            {
                if (unitDisplayedModules[unit].isTaken) continue;

                RemoveModule(unit);
                ModuleInitHandler(unit, modGen.GetRandomModuleForUnit(unit));
            }
        }
    }

    private void ModuleInitHandler(UnitClass TargetUnitClass, ModuleBaseSO moduleSO)
    {
        var box = unitAssignedBox[TargetUnitClass];
        var module = new Module(moduleSO, box, texturesSO);

        module.moduleCase.clicked += () => SelectModule(module);
        unitDisplayedModules.Add(TargetUnitClass, module);
    }

    private void RemoveModule(UnitClass unit)
    {
        unitDisplayedModules[unit].moduleCase.RemoveFromHierarchy();
        unitDisplayedModules.Remove(unit);
    }

    private void SelectModule(Module module)
    {
        UnitClass unit = UnitClass.Robot;
        foreach (var kvp in unitDisplayedModules)
        {
            if (kvp.Value == module)
            {
                unit = kvp.Key;
                break;
            }
        }

        ModuleManager.Instance.AddNewModuleToDict(unit, module.moduleConfig);

        module.isTaken = true;
        module.moduleCase.SetEnabled(false);

        GenerateModules();
        takenModules++;

        UpdateModuleScreen();

        if (takenModules < modulesPerWave) return;
        HideModuleBoard();
    }

    private void UpdateModuleScreen()
    {
        tokenModuleTracker.text = "ML: " + (modulesPerWave - takenModules) + "MT: " + modulationTokens;

        if (modulationTokens <= 0)
        {
            stabilizeButton.SetEnabled(false);
            recalibrateButton.SetEnabled(false);
        }
        else
        {
            stabilizeButton.SetEnabled(true);

            if ((modulesPerWave - takenModules) <= 1)
                stabilizeButton.SetEnabled(false);
            else
                recalibrateButton.SetEnabled(true);
        }
    }


    private class Module
    {
        public ModuleBaseSO moduleConfig;
        public VisualElement moduleBox;
        public ModuleSelectorTexturesSO texturesSO;
        public bool isTaken = false;

        //Elements
        public Button moduleCase;
        public VisualElement moduleBG;
        public VisualElement[] moduleBGCOLayers = new VisualElement[4];
        public VisualElement wideIcon;
        public VisualElement textBox;
        public Label description;
        public Label tier;
        public Label buff;


        public Module(ModuleBaseSO moduleConfig, VisualElement moduleBox, ModuleSelectorTexturesSO texturesSO)
        {
            this.moduleConfig = moduleConfig;
            this.moduleBox = moduleBox;
            this.texturesSO = texturesSO;

            InitializeModule();
        }

        private void InitializeModule()
        {
            var UICtrlMng = UIControllerManager.Instance;

            Color colorMain = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => UICtrlMng.colorScheme.WPOrange,
                ModuleCategory.TacticalSystem => UICtrlMng.colorScheme.TSBlue,
                ModuleCategory.DefensiveProtocol => UICtrlMng.colorScheme.DPGreen,
                _ => Color.white
            };

            Color colorBG = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => UICtrlMng.colorScheme.WPOrangeBG,
                ModuleCategory.TacticalSystem => UICtrlMng.colorScheme.TSBlueBG,
                ModuleCategory.DefensiveProtocol => UICtrlMng.colorScheme.DPGreenBG,
                _ => Color.gray
            };

            moduleCase = UITK.AddElement<Button>(moduleBox, "ClearButton", "moduleCase", "P");

            moduleBG = UITK.AddElement(moduleCase, "moduleBG");
            moduleBGCOLayers = UITK.CreateChromaticAberration(moduleBG, texturesSO.moduleCaseMask, colorBG, 15f);

            wideIcon = UITK.AddElement(moduleCase, "wideIcon");
            wideIcon.style.backgroundColor = colorMain;
            wideIcon.style.backgroundImage = moduleConfig.wideIcon;

            textBox = UITK.AddElement(moduleCase, "textBox");
            textBox.style.backgroundColor = colorMain;

            description = UITK.AddElement<Label>(textBox, "description");
            description.style.color = UICtrlMng.colorScheme.white;
            description.text = moduleConfig.description;

            tier = UITK.AddElement<Label>(moduleCase, "tier", "H2");
            tier.style.color = UICtrlMng.colorScheme.white;
            tier.text = moduleConfig.tier switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                _ => "0"
            };

            buff = UITK.AddElement<Label>(moduleCase, "buff", "H2");
            buff.style.color = UICtrlMng.colorScheme.white;
            buff.text = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => moduleConfig.tier switch
                {
                    1 => "10% ÓÐÎÍ",
                    2 => "20% ÓÐÎÍ",
                    3 => "40% ÓÐÎÍ",
                    _ => "ERROR"
                },

                ModuleCategory.TacticalSystem => moduleConfig.tier switch
                {
                    1 => "10 ÌÎÙÜ",
                    2 => "20 ÌÎÙÜ",
                    3 => "40 ÌÎÙÜ",
                    _ => "ERROR"
                },

                ModuleCategory.DefensiveProtocol => moduleConfig.tier switch
                {
                    1 => "10% ÇÄÐÂ.",
                    2 => "20% ÇÄÐÂ.",
                    3 => "40% ÇÄÐÂ.",
                    _ => "ERROR"
                },

                _ => "ERROR"
            };
        }
    }


    private void Awake()
    {
        InitializeUI();
    }

    private void Update()
    {
        foreach (var module in unitDisplayedModules.Values)
        {
            UITK.ParallaxOffset(module.moduleCase, Input.mousePosition, 0.007f);
            UITK.ParallaxOffset(module.moduleBGCOLayers[0], Input.mousePosition, 0.007f);
            UITK.ParallaxOffset(module.moduleBGCOLayers[2], Input.mousePosition, 0.014f);
        }
    }
}
