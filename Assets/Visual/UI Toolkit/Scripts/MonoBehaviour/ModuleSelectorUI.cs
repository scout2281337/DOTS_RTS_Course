using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ModuleSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ModuleSelectorTexturesSO texturesSO;

    private VisualElement moduleBoard;
    private Dictionary<UnitClass, Module> unitDisplayedModules = new();
    private Dictionary<UnitClass, bool> unitTakenModules = new();

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

        moduleBoard = UITK.AddElement(root, "moduleBoard", "P");

        var upgradeButton = UITK.AddElement<Button>(moduleBoard, "upgradeButton");
        upgradeButton.clicked += GenerateModules;

        var recalibrateButton = UITK.AddElement<Button>(moduleBoard, "recalibrateButton");


        var stabilizeButton = UITK.AddElement<Button>(moduleBoard, "stabilizeButton");
    }

    private void ModuleInitHandler(UnitClass TargetUnitClass, ModuleBaseSO moduleSO)
    {
        var module = new Module(moduleBoard, moduleSO, texturesSO);

        unitDisplayedModules.Add(TargetUnitClass, module);
    }

    private void GenerateModules()
    {
        //First generation
        if (unitDisplayedModules.Count == 0)
        {
            foreach (var unit in FriendlyUnitManager.Instance.unitEntityDict.Keys)
            {
                ModuleInitHandler(unit, ModuleGenerator.Instance.GetRandomModule());
                unitTakenModules.Add(unit, false);
            }
        }
        //Recalibration
        else
        {
            foreach (var unit in FriendlyUnitManager.Instance.unitEntityDict.Keys)
            {
                if (unitTakenModules[unit]) continue;

                unitDisplayedModules[unit].moduleCase.RemoveFromHierarchy();
                unitDisplayedModules.Remove(unit);
                ModuleInitHandler(unit, ModuleGenerator.Instance.GetRandomModule());
            }
        }
    }

    private class Module
    {
        public VisualElement moduleBoard;
        public VisualElement moduleCase;
        public VisualElement[] moduleBGCOLayers = new VisualElement[4];
        public ModuleSelectorTexturesSO texturesSO;
        public ModuleBaseSO moduleConfig;

        public bool isLocked = false;


        public Module(VisualElement moduleBoard, ModuleBaseSO moduleConfig, ModuleSelectorTexturesSO texturesSO) 
        {
            this.moduleBoard = moduleBoard;
            this.moduleConfig = moduleConfig;
            this.texturesSO = texturesSO;

            InitializeModule();
        }

        public void InitializeModule()
        {
            Color colorMain = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => UIControllerManager.Instance.colorScheme.WPOrange,
                ModuleCategory.TacticalSystem => UIControllerManager.Instance.colorScheme.TSBlue,
                ModuleCategory.DefensiveProtocol => UIControllerManager.Instance.colorScheme.DPGreen,
                _ => Color.white
            };

            Color colorBG = moduleConfig.category switch
            {
                ModuleCategory.WeaponPower => UIControllerManager.Instance.colorScheme.WPOrangeBG,
                ModuleCategory.TacticalSystem => UIControllerManager.Instance.colorScheme.TSBlueBG,
                ModuleCategory.DefensiveProtocol => UIControllerManager.Instance.colorScheme.DPGreenBG,
                _ => Color.gray
            };

            moduleCase = UITK.AddElement(moduleBoard, "moduleCase");

            var moduleBG = UITK.AddElement(moduleCase, "moduleBG");
            moduleBGCOLayers = UITK.CreateChromaticAberration(moduleBG, texturesSO.moduleCaseMask, colorBG, 15f);

            var wideIcon = UITK.AddElement(moduleCase, "wideIcon");
            wideIcon.style.backgroundColor = colorMain;
            wideIcon.style.backgroundImage = moduleConfig.wideIcon;

            var textBox = UITK.AddElement(moduleCase, "textBox");
            textBox.style.backgroundColor = colorMain;

            var description = UITK.AddElement<Label>(textBox, "description");
            description.style.color = UIControllerManager.Instance.colorScheme.white;
            description.text = moduleConfig.description;

            var tier = UITK.AddElement<Label>(moduleCase, "tier", "H2");
            tier.style.color = UIControllerManager.Instance.colorScheme.white;
            tier.text = moduleConfig.tier switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                _ => "0"
            };

            var buff = UITK.AddElement<Label>(moduleCase, "buff", "H2");
            buff.style.color = UIControllerManager.Instance.colorScheme.white;
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
