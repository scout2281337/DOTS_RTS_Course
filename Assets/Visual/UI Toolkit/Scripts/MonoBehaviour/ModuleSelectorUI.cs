using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using UnityEngine.UIElements;

public class ModuleSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ModuleSelectorTexturesSO texturesSO;

    private VisualElement moduleBoard;
    private Module[] modules = new Module[4];

    public ModuleBaseSO[] moduleConfigTester;



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

        modules[0] = new Module(moduleBoard, moduleConfigTester[0], texturesSO);
        modules[1] = new Module(moduleBoard, moduleConfigTester[1], texturesSO);
        modules[2] = new Module(moduleBoard, moduleConfigTester[2], texturesSO);
        modules[3] = new Module(moduleBoard, moduleConfigTester[3], texturesSO);
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
                    1 => "1 ÌÎÙÜ",
                    2 => "2 ÌÎÙÜ",
                    3 => "4 ÌÎÙÜ",
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
        for (int i = 0; modules.Length > i; i++)
        {
            UITK.ParallaxOffset(modules[i].moduleCase, Input.mousePosition, 0.007f);
            UITK.ParallaxOffset(modules[i].moduleBGCOLayers[0], Input.mousePosition, 0.007f);
            UITK.ParallaxOffset(modules[i].moduleBGCOLayers[2], Input.mousePosition, 0.014f);
        }
    }
}
