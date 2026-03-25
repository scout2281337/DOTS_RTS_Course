using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;

    [SerializeField] private SoldierAttributeGroupConfig[] classSets;


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

        var setPanel = UITK.AddElement(root, "setPanel");
        var clr = UICtrlMng.colorScheme.darkGray;
        setPanel.style.backgroundColor = new Color(clr.r, clr.g, clr.b, 0.5f);
        setPanel.style.borderBottomColor = UICtrlMng.colorScheme.lightGray;
        setPanel.style.borderLeftColor = UICtrlMng.colorScheme.lightGray;
        setPanel.style.borderRightColor = UICtrlMng.colorScheme.lightGray;
        setPanel.style.borderTopColor = UICtrlMng.colorScheme.lightGray;

        for (int i = 0; i < 3; i++)
        {
            InitializeSetBox(setPanel);
        }
    }

    private void InitializeSetBox(VisualElement setPanel)
    {
        var UICtrlMng = UIControllerManager.Instance;

        var setElementBox = UITK.AddElement(setPanel, "setElementBox");


        var buttonBox = UITK.AddElement(setElementBox, "buttonBox");

        for (int j = 0; j < 2; j++)
        {
            var setElementButton = UITK.AddElement<Button>(buttonBox, "setElementButton");
            setElementButton.style.backgroundColor = UICtrlMng.colorScheme.DPGreenBG;
            setElementButton.style.color = UICtrlMng.colorScheme.white;
            setElementButton.text = classSets[0].bodyConfig.attributeName;
        }


        var setElementDescription = UITK.AddElement<Label>(setElementBox, "setElementDescription");
        setElementDescription.style.backgroundColor = UICtrlMng.colorScheme.DPGreenBG;
        setElementDescription.style.color = UICtrlMng.colorScheme.white;
        setElementDescription.text = classSets[0].bodyConfig.attributeDescription;

        var miscBox = UITK.AddElement(setElementBox, "miscBox");
        miscBox.style.backgroundColor = UICtrlMng.colorScheme.DPGreenBG;


        var classArmor = UITK.AddElement<Label>(miscBox, "classArmor");
        classArmor.style.color = UICtrlMng.colorScheme.white;
        classArmor.text = "Armor: " + classSets[0].bodyConfig.armor;

        var classSpeed = UITK.AddElement<Label>(miscBox, "classSpeed");
        classSpeed.style.color = UICtrlMng.colorScheme.white;
        classSpeed.text = "Speed: " + classSets[0].bodyConfig.speed;
    }


    private void Awake()
    {
        InitializeUI();
    }
}
