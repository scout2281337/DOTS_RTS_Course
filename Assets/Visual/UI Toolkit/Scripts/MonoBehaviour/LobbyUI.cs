using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;

    [SerializeField] private SoldierAttributeGroupConfig[] attributeGroups;


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

        var soldierPanel = UITK.AddElement(root, "soldierPanel");
        var clr = UICtrlMng.colorScheme.darkGray;
        soldierPanel.style.backgroundColor = new Color(clr.r, clr.g, clr.b, 0.5f);
        soldierPanel.style.borderBottomColor = UICtrlMng.colorScheme.lightGray;
        soldierPanel.style.borderLeftColor = UICtrlMng.colorScheme.lightGray;
        soldierPanel.style.borderRightColor = UICtrlMng.colorScheme.lightGray;
        soldierPanel.style.borderTopColor = UICtrlMng.colorScheme.lightGray;

        InitializeAttributeBox(soldierPanel, attributeGroups[0].bodyConfig);
        InitializeAttributeBox(soldierPanel, attributeGroups[0].weaponConfigs);
        InitializeAttributeBox(soldierPanel, attributeGroups[0].skillConfigs);

    }

    private void InitializeAttributeBox(VisualElement soldierPanel, params BaseSoldierAttribute[] attributes)
    {
        var UICtrlMng = UIControllerManager.Instance;
        attributes[0].GetAttributeLobbyBox(UICtrlMng.colorScheme.DPGreenBG, UICtrlMng.colorScheme.white,
           out Button attributeButton, out Label attributeDescription, out VisualElement miscBox);  

        var attributeBox = UITK.AddElement(soldierPanel, "attributeBox");

        var buttonBox = UITK.AddElement(attributeBox, "buttonBox");

        buttonBox.Add(attributeButton);

        attributeBox.Add(attributeDescription);

        attributeBox.Add(miscBox);
    }


    private void Awake()
    {
        InitializeUI();
    }
}
