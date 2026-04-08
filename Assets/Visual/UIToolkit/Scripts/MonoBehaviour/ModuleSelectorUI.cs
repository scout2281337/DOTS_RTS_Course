using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModuleSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ViewController UIController;
    [SerializeField] private ModuleSelectorTexturesSO texturesSO;

    [SerializeField] private int modulesPerWave = 3;
    [SerializeField] private int modulationTokens = 3;

    private bool isUpgradable = true;
    //private bool isHidden = false;
    private bool isStabilized = false;
    private int takenModules = 0;
    private Dictionary<UnitClass, VisualElement> unitAssignedBox = new();
    private Dictionary<UnitClass, Module> unitDisplayedModules = new();

    private VisualElement moduleScreenBG;
    private VisualElement moduleBoard;
    private VisualElement[] moduleBoxes;
    private Button upgradeButton;
    private Button recalibrateButton;
    private Button stabilizeButton;
    private Label tokenModuleTracker;


    private void BuildModuleSelector()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in UIController.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        moduleScreenBG = UITK.AddElement(root, "moduleScreenBG");

        var topSection = UITK.AddElement(root, "topSection");

        upgradeButton = UITK.AddElement<Button>(topSection, "PrimaryButton", "H1", "upgradeButton");
        upgradeButton.text = "UPGRADE";
        upgradeButton.clicked += StartUpgrade;

        tokenModuleTracker = UITK.AddElement<Label>(topSection, "H4", "tokenModuleTracker");
        tokenModuleTracker.text = "ML: " + modulesPerWave + "   MT: " + modulationTokens;

        var midSection = UITK.AddElement(root, "midSection");

        var recalibrateButtonBox = UITK.AddElement(midSection, "recalibrateButtonBox"); // Need it for proper alignment due to rotation of the button
        recalibrateButton = UITK.AddElement<Button>(recalibrateButtonBox, "SecondaryButton", "H4", "recalibrateButton");
        recalibrateButton.text = "RECALIBRATE";
        recalibrateButton.clicked += RecalibrateModules;

        moduleBoard = UITK.AddElement(midSection, "moduleBoard");

        var stabilizeButtonBox = UITK.AddElement(midSection, "stabilizeButtonBox");  // Need it for proper alignment due to rotation of the button
        stabilizeButton = UITK.AddElement<Button>(stabilizeButtonBox, "SecondaryButton", "H4", "stabilizeButton");
        stabilizeButton.text = "STABILIZE";
        stabilizeButton.clicked += StabilizeModules;

        HideModuleBoard();

        upgradeButton.style.display = DisplayStyle.Flex;
    }

    private void StartUpgrade()
    {
        RevealModuleBoard();
        upgradeButton.style.display = DisplayStyle.None;

        GenerateModules();
    }

    private void RecalibrateModules()
    {
        if (modulationTokens <= 0) return;
        if (isStabilized) return;

        RecycleModules();
        modulationTokens--;

        UpdateModuleScreen();
    }

    private void StabilizeModules()
    {
        if (isStabilized)
        {
            isStabilized = false;
            modulationTokens++;
        }
        else
        {
            if (modulationTokens <= 0) return;
            isStabilized = true;
            modulationTokens--;
        }

        UpdateModuleScreen();
    }

    private void RevealModuleBoard()
    {
        moduleBoard.style.display = DisplayStyle.Flex;
        recalibrateButton.style.display = DisplayStyle.Flex;
        stabilizeButton.style.display = DisplayStyle.Flex;
        tokenModuleTracker.style.display = DisplayStyle.Flex;
        moduleScreenBG.style.display = DisplayStyle.Flex;

        UpdateModuleScreen();

        //isHidden = false;
    }

    private void HideModuleBoard()
    {
        moduleBoard.style.display = DisplayStyle.None;
        recalibrateButton.style.display = DisplayStyle.None;
        stabilizeButton.style.display = DisplayStyle.None;
        tokenModuleTracker.style.display = DisplayStyle.None;
        moduleScreenBG.style.display = DisplayStyle.None;

        //isHidden = true;
    }

    private void GenerateModules()
    {
        if (!isUpgradable) return;
        isUpgradable = false;


        var unitDict = FriendlyUnitManager.Instance.unitEntityDict;
        var modGen = ModuleManager.Instance;
        
        moduleBoxes = new VisualElement[unitDict.Count];

        int i = 0;
        foreach (var unit in unitDict.Keys)
        {
            var box = UITK.AddElement(moduleBoard, "moduleBox");

            moduleBoxes[i++] = box;
            unitAssignedBox.Add(unit, box);

            ModuleBuilder(unit, modGen.GetRandomModuleForUnit(unit));
        }

        Debug.Log(moduleBoxes);
    }

    private void RecycleModules()
    {
        if (isStabilized)
        {
            isStabilized = false;
            return;
        }

        var unitDict = FriendlyUnitManager.Instance.unitEntityDict;
        var modGen = ModuleManager.Instance;

        foreach (var unit in unitDict.Keys)
        {
            if (unitDisplayedModules[unit].isTaken) continue;

            RemoveModule(unit);
            ModuleBuilder(unit, modGen.GetRandomModuleForUnit(unit));
        }
    }

    private void ModuleBuilder(UnitClass TargetUnitClass, ModuleBaseSO moduleSO)
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

        RecycleModules();
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
        public VisualElement wideIcon;
        public VisualElement textBox;
        public Label description;
        public Label tier;
        public Label buff;
        public VisualElement[] moduleCOLayers;


        public Module(ModuleBaseSO moduleConfig, VisualElement moduleBox, ModuleSelectorTexturesSO texturesSO)
        {
            this.moduleConfig = moduleConfig;
            this.moduleBox = moduleBox;
            this.texturesSO = texturesSO;

            BuildModule();
        }

        private void BuildModule()
        {
            string colorClass = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => "WP",
                ModuleCategory.TacticalSystem => "TS",
                ModuleCategory.DefensiveProtocol => "DP",
                _ => "WP"
            };

            moduleCase = UITK.AddElement<Button>(moduleBox, "ClearButton", "moduleCase");

            moduleBG = UITK.AddElement(moduleCase, colorClass, "moduleBG");
            moduleBG.style.backgroundImage = texturesSO.moduleCaseMask;

            moduleCOLayers = UITK.CreateChromaticAberration(moduleBG, 0.7f);

            wideIcon = UITK.AddElement(moduleCase, colorClass, "wideIcon");
            wideIcon.style.backgroundImage = moduleConfig.wideIcon;

            textBox = UITK.AddElement(moduleCase, colorClass, "textBox");

            description = UITK.AddElement<Label>(textBox, "P2", "description");
            description.text = moduleConfig.description;

            tier = UITK.AddElement<Label>(moduleCase, "tier", "P1");
            tier.text = moduleConfig.tier switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                _ => "0"
            };

            buff = UITK.AddElement<Label>(moduleCase, "buff", "P1");
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
        BuildModuleSelector();
    }

    private void Update()
    {
        foreach (var module in unitDisplayedModules.Values)
        {
            UITK.ParallaxOffset(module.moduleCase, Input.mousePosition, 0.008f);
            UITK.ParallaxOffset(module.moduleCOLayers[0], Input.mousePosition, 0.008f);
            UITK.ParallaxOffset(module.moduleCOLayers[2], Input.mousePosition, 0.016f);
        }
    }
}
