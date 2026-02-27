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

        modules[0] = new Module(moduleBoard, texturesSO);
        modules[1] = new Module(moduleBoard, texturesSO);
        modules[2] = new Module(moduleBoard, texturesSO);
        modules[3] = new Module(moduleBoard, texturesSO);
    }

    private class Module
    {
        public VisualElement moduleBoard;
        public VisualElement moduleCase;
        public VisualElement[] moduleBGCOLayers = new VisualElement[4];
        public ModuleSelectorTexturesSO texturesSO;

        public bool isLocked = false;


        public Module(VisualElement moduleBoard, ModuleSelectorTexturesSO texturesSO) 
        {
            this.moduleBoard = moduleBoard;
            this.texturesSO = texturesSO;

            InitializeModule();
        }

        public void InitializeModule()
        {
            moduleCase = UITK.AddElement(moduleBoard, "moduleCase");

            var moduleBG = UITK.AddElement(moduleCase, "moduleBG");
            moduleBGCOLayers = UITK.CreateChromaticAberration(moduleBG, texturesSO.moduleCaseMask, 
                UIControllerManager.Instance.colorScheme.WPOrangeBG, 15f);
            //moduleBG.style.backgroundImage = texturesSO.moduleCaseMask;
            //moduleBG.style.unityBackgroundImageTintColor = UIControllerManager.Instance.colorScheme.WPOrangeBG;

            var wideIcon = UITK.AddElement(moduleCase, "wideIcon");
            wideIcon.style.backgroundColor = UIControllerManager.Instance.colorScheme.WPOrange;

            var textBox = UITK.AddElement(moduleCase, "textBox");
            textBox.style.backgroundColor = UIControllerManager.Instance.colorScheme.WPOrange;

            var description = UITK.AddElement<Label>(textBox, "description");
            description.style.color = UIControllerManager.Instance.colorScheme.white;
            description.text = "/Триггер/ Здоровье находиться на отметке 0-50%\r\n/Эффект: Постоянный/ Увеличивает урон до 50%. Чем меньше здоровья тем больше бонус.";

            var tier = UITK.AddElement<Label>(moduleCase, "tier", "H2");
            tier.style.color = UIControllerManager.Instance.colorScheme.white;
            tier.text = "III";

            var buff = UITK.AddElement<Label>(moduleCase, "buff", "H2");
            buff.style.color = UIControllerManager.Instance.colorScheme.white;
            buff.text = "10% УРОН";
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
