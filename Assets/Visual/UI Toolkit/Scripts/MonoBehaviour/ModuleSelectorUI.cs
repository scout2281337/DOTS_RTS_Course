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
            moduleCase.style.backgroundImage = texturesSO.moduleCaseMask;
            moduleCase.style.unityBackgroundImageTintColor = UIControllerManager.Instance.colorScheme.WPOrangeBG;

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

    private void ParallaxOffset(VisualElement element, Vector2 mousePos)
    {
        Vector2 elementCenterInPanel = element.worldBound.center;
        Vector2 mousePositionInPanel = new(Input.mousePosition.x, 1080 - Input.mousePosition.y);

        Vector2 offsetFromMouse = mousePositionInPanel - elementCenterInPanel;
        Vector2 parallaxOffset = offsetFromMouse * -0.01f;

        element.style.left = parallaxOffset.x;
        element.style.top = parallaxOffset.y;
    }


    private void Awake()
    {
        InitializeUI();
    }

    private void Update()
    {
        ParallaxOffset(modules[0].moduleCase, Input.mousePosition);
    }
}
