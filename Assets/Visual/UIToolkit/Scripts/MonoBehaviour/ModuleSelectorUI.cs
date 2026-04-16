using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModuleSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private ModuleSelectorTexturesSO _texturesSO;

    [SerializeField] private int _modulesPerWave = 3;
    [SerializeField] private int _modulationTokens = 3;

    private bool _isUpgradable = true;
    //private bool isHidden = false;
    private bool _isStabilized = false;
    private int _takenModules = 0;
    private Dictionary<UnitClass, VisualElement> _unitAssignedBox = new();
    private Dictionary<UnitClass, Module> _unitDisplayedModules = new();

    private VisualElement _moduleScreenBG;
    private VisualElement _moduleBoard;
    private VisualElement[] _moduleBoxes;
    private Button _upgradeButton;
    private Button _recalibrateButton;
    private Button _stabilizeButton;
    private Label _tokenModuleTracker;


    private void BuildModuleSelector()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        root.Clear();

        ViewController _UIController = ViewController.Instance;
        foreach (StyleSheet sheet in _UIController.DefaultStyleSheet.BaseStyles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in _styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        _moduleScreenBG = UITK.AddElement(root, "moduleScreenBG");

        var topSection = UITK.AddElement(root, "topSection");

        _upgradeButton = UITK.AddElement<Button>(topSection, "PrimaryButton", "H1", "upgradeButton");
        _upgradeButton.text = "UPGRADE";
        _upgradeButton.clicked += StartUpgrade;

        _tokenModuleTracker = UITK.AddElement<Label>(topSection, "H4", "tokenModuleTracker");
        _tokenModuleTracker.text = "ML: " + _modulesPerWave + "   MT: " + _modulationTokens;

        var midSection = UITK.AddElement(root, "midSection");

        var recalibrateButtonBox = UITK.AddElement(midSection, "recalibrateButtonBox"); // Need it for proper alignment due to rotation of the button
        _recalibrateButton = UITK.AddElement<Button>(recalibrateButtonBox, "SecondaryButton", "H4", "recalibrateButton");
        _recalibrateButton.text = "RECALIBRATE";
        _recalibrateButton.clicked += RecalibrateModules;

        _moduleBoard = UITK.AddElement(midSection, "moduleBoard");

        var stabilizeButtonBox = UITK.AddElement(midSection, "stabilizeButtonBox");  // Need it for proper alignment due to rotation of the button
        _stabilizeButton = UITK.AddElement<Button>(stabilizeButtonBox, "SecondaryButton", "H4", "stabilizeButton");
        _stabilizeButton.text = "STABILIZE";
        _stabilizeButton.clicked += StabilizeModules;

        HideModuleBoard();

        _upgradeButton.style.display = DisplayStyle.Flex;
    }

    private void StartUpgrade()
    {
        RevealModuleBoard();
        _upgradeButton.style.display = DisplayStyle.None;

        GenerateModules();
    }

    private void RecalibrateModules()
    {
        if (_modulationTokens <= 0) return;
        if (_isStabilized) return;

        RecycleModules();
        _modulationTokens--;

        UpdateModuleScreen();
    }

    private void StabilizeModules()
    {
        if (_isStabilized)
        {
            _isStabilized = false;
            _modulationTokens++;
        }
        else
        {
            if (_modulationTokens <= 0) return;
            _isStabilized = true;
            _modulationTokens--;
        }

        UpdateModuleScreen();
    }

    private void RevealModuleBoard()
    {
        _moduleBoard.style.display = DisplayStyle.Flex;
        _recalibrateButton.style.display = DisplayStyle.Flex;
        _stabilizeButton.style.display = DisplayStyle.Flex;
        _tokenModuleTracker.style.display = DisplayStyle.Flex;
        _moduleScreenBG.style.display = DisplayStyle.Flex;

        UpdateModuleScreen();

        //isHidden = false;
    }

    private void HideModuleBoard()
    {
        _moduleBoard.style.display = DisplayStyle.None;
        _recalibrateButton.style.display = DisplayStyle.None;
        _stabilizeButton.style.display = DisplayStyle.None;
        _tokenModuleTracker.style.display = DisplayStyle.None;
        _moduleScreenBG.style.display = DisplayStyle.None;

        //isHidden = true;
    }

    private void GenerateModules()
    {
        if (!_isUpgradable) return;
        _isUpgradable = false;


        var unitDict = FriendlyUnitManager.Instance.unitEntityDict;
        var modGen = ModuleManager.Instance;
        
        _moduleBoxes = new VisualElement[unitDict.Count];

        int i = 0;
        foreach (var unit in unitDict.Keys)
        {
            var box = UITK.AddElement(_moduleBoard, "moduleBox");

            _moduleBoxes[i++] = box;
            _unitAssignedBox.Add(unit, box);

            ModuleBuilder(unit, modGen.GetRandomModuleForUnit(unit));
        }

        Debug.Log(_moduleBoxes);
    }

    private void RecycleModules()
    {
        if (_isStabilized)
        {
            _isStabilized = false;
            return;
        }

        var unitDict = FriendlyUnitManager.Instance.unitEntityDict;
        var modGen = ModuleManager.Instance;

        foreach (var unit in unitDict.Keys)
        {
            if (_unitDisplayedModules[unit].isTaken) continue;

            RemoveModule(unit);
            ModuleBuilder(unit, modGen.GetRandomModuleForUnit(unit));
        }
    }

    private void ModuleBuilder(UnitClass TargetUnitClass, ModuleBaseSO moduleSO)
    {
        var box = _unitAssignedBox[TargetUnitClass];
        var module = new Module(moduleSO, box, _texturesSO);

        module.moduleCase.clicked += () => SelectModule(module);
        _unitDisplayedModules.Add(TargetUnitClass, module);
    }

    private void RemoveModule(UnitClass unit)
    {
        _unitDisplayedModules[unit].moduleCase.RemoveFromHierarchy();
        _unitDisplayedModules.Remove(unit);
    }

    private void SelectModule(Module module)
    {
        UnitClass unit = UnitClass.Robot;
        foreach (var kvp in _unitDisplayedModules)
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
        _takenModules++;

        UpdateModuleScreen();

        if (_takenModules < _modulesPerWave) return;
        HideModuleBoard();
    }

    private void UpdateModuleScreen()
    {
        _tokenModuleTracker.text = "ML: " + (_modulesPerWave - _takenModules) + "MT: " + _modulationTokens;

        if (_modulationTokens <= 0)
        {
            _stabilizeButton.SetEnabled(false);
            _recalibrateButton.SetEnabled(false);
        }
        else
        {
            _stabilizeButton.SetEnabled(true);

            if ((_modulesPerWave - _takenModules) <= 1)
                _stabilizeButton.SetEnabled(false);
            else
                _recalibrateButton.SetEnabled(true);
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
            moduleBG.style.backgroundImage = texturesSO.ModuleCaseMask;

            moduleCOLayers = UITK.CreateChromaticAberration(moduleBG, 0.7f);

            wideIcon = UITK.AddElement(moduleCase, colorClass, "wideIcon");
            wideIcon.style.backgroundImage = moduleConfig.wideIcon;

            textBox = UITK.AddElement(moduleCase, colorClass, "textBox");

            description = UITK.AddElement<Label>(textBox, "P3", "description");
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
        foreach (var module in _unitDisplayedModules.Values)
        {
            UITK.ParallaxOffset(module.moduleCase, Input.mousePosition, 0.008f);
            UITK.ParallaxOffset(module.moduleCOLayers[0], Input.mousePosition, 0.008f);
            UITK.ParallaxOffset(module.moduleCOLayers[2], Input.mousePosition, 0.016f);
        }
    }
}
