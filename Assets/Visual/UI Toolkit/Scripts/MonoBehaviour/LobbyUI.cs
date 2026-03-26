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

        UIControllerManager UICtrlMng = UIControllerManager.Instance;
        var clrBG = UICtrlMng.colorScheme.darkGray;

        foreach (StyleSheet sheet in UICtrlMng.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        // SoldiersPanel setup
        var soldiersPanel = UITK.AddElement(root, "soldiersPanel");
        soldiersPanel.style.backgroundColor = new Color(clrBG.r, clrBG.g, clrBG.b, 0.5f);
        soldiersPanel.style.borderBottomColor = UICtrlMng.colorScheme.lightGray;
        soldiersPanel.style.borderLeftColor = UICtrlMng.colorScheme.lightGray;
        soldiersPanel.style.borderRightColor = UICtrlMng.colorScheme.lightGray;
        soldiersPanel.style.borderTopColor = UICtrlMng.colorScheme.lightGray;

        // Columns for displaying icons 
        VisualElement[] iconColumns = new VisualElement[2];
        for (int i = 0; i < 2; i++)
        {
            iconColumns[i] = UITK.AddElement(soldiersPanel, "iconColumn");
        }

        // Creating buttons and layout
        Button[] soldierIcons = new Button[attributeGroups.Length];
        for (int i = 0; i < soldierIcons.Length; i++)
        {
            // Placing icons in right column
            var column = i % 2 == 0 ? iconColumns[0] : iconColumns[1];

            soldierIcons[i] = UITK.AddElement<Button>(column, "soldierIcon");
            soldierIcons[i].style.backgroundImage = attributeGroups[i].icon;
        }

        // AttributePanel setup
        var attributesPanel = UITK.AddElement(root, "attributesPanel");
        attributesPanel.style.backgroundColor = new Color(clrBG.r, clrBG.g, clrBG.b, 0.5f);
        attributesPanel.style.borderBottomColor = UICtrlMng.colorScheme.lightGray;
        attributesPanel.style.borderLeftColor = UICtrlMng.colorScheme.lightGray;
        attributesPanel.style.borderRightColor = UICtrlMng.colorScheme.lightGray;
        attributesPanel.style.borderTopColor = UICtrlMng.colorScheme.lightGray;

        // Creating containers and hiding them, so that there is no overlap between each over
        AttributesContainer[] attributesContainers = new AttributesContainer[attributeGroups.Length];
        for (int i = 0; i < attributeGroups.Length; i++)
        {
            attributesContainers[i] = new AttributesContainer(attributeGroups[i], attributesPanel);
            attributesContainers[i].Deactivate();
        }

        // Panel switching
        for (int i = 0; i < attributesContainers.Length; i++)
        {
            soldierIcons[i].clicked += attributesContainers[i].Activate;
            for (int j = 0; j < attributesContainers.Length; j++)
            {
                if (i == j) continue;

                soldierIcons[i].clicked += attributesContainers[j].Deactivate;
            }
        }

        // TeamPanel setup
        var teamPanel = UITK.AddElement(root, "teamPanel");

        for (int i = 0; i < 4; i++)
        {
            var memberField = UITK.AddElement<Button>(teamPanel, "memberField");
        }
    }


    private class AttributesContainer
    {
        public VisualElement bodyAttributeBox;
        public AttributeTab[] bodyAttributeTabs;
        public VisualElement weaponAttributeBox;
        public AttributeTab[] weaponAttributeTabs;
        public VisualElement skillAttributeBox;
        public AttributeTab[] skillAttributeTabs;
        public bool isActive = true;

        SoldierAttributeGroupConfig attributeGroup;
        VisualElement attributesPanel;


        public AttributesContainer(SoldierAttributeGroupConfig attributeGroup, VisualElement attributesPanel)
        {
            this.attributeGroup = attributeGroup;
            this.attributesPanel = attributesPanel;

            bodyAttributeBox = InitializeAttributeBox(attributesPanel, out bodyAttributeTabs, attributeGroup.bodyConfig);
            weaponAttributeBox = InitializeAttributeBox(attributesPanel, out weaponAttributeTabs, attributeGroup.weaponConfigs);
            skillAttributeBox = InitializeAttributeBox(attributesPanel, out skillAttributeTabs, attributeGroup.skillConfigs);
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            // Deactivating all tabs except first one, so that there is no overlap
            // We also have to activate attributeBoxes so that layout displays correctly
            // We have to manually activate AttributeButton, because it stays disabled after calling attributeTab.Activate
            bodyAttributeTabs[0].Activate();
            bodyAttributeBox.style.display = DisplayStyle.Flex;
            bodyAttributeTabs[0].attributeButton.style.display = DisplayStyle.Flex;
            for (int i = 1; i < bodyAttributeTabs.Length; i++)
            {
                bodyAttributeTabs[i].Deactivate();
                bodyAttributeTabs[i].attributeButton.style.display = DisplayStyle.Flex;
            }

            weaponAttributeTabs[0].Activate();
            weaponAttributeBox.style.display = DisplayStyle.Flex;
            weaponAttributeTabs[0].attributeButton.style.display = DisplayStyle.Flex;
            for (int i = 1; i < weaponAttributeTabs.Length; i++)
            {
                weaponAttributeTabs[i].Deactivate();
                weaponAttributeTabs[i].attributeButton.style.display = DisplayStyle.Flex;
            }

            skillAttributeTabs[0].Activate();
            skillAttributeBox.style.display = DisplayStyle.Flex;
            skillAttributeTabs[0].attributeButton.style.display = DisplayStyle.Flex;
            for (int i = 1; i < skillAttributeTabs.Length; i++)
            {
                skillAttributeTabs[i].Deactivate();
                skillAttributeTabs[i].attributeButton.style.display = DisplayStyle.Flex;
            }
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;


            // We also have to disable attributeBoxes so that layout displays correctly
            // We have to manually disable AttributeButton, because it stays active after calling attributeTab.deactivate
            bodyAttributeBox.style.display = DisplayStyle.None;
            for (int i = 0; i < bodyAttributeTabs.Length; i++)
            {
                bodyAttributeTabs[i].Deactivate();
                bodyAttributeTabs[i].attributeButton.style.display = DisplayStyle.None;
            }

            weaponAttributeBox.style.display = DisplayStyle.None;
            for (int i = 0; i < weaponAttributeTabs.Length; i++)
            {
                weaponAttributeTabs[i].Deactivate();
                weaponAttributeTabs[i].attributeButton.style.display = DisplayStyle.None;
            }

            skillAttributeBox.style.display = DisplayStyle.None;
            for (int i = 0; i < skillAttributeTabs.Length; i++)
            {
                skillAttributeTabs[i].Deactivate();
                skillAttributeTabs[i].attributeButton.style.display = DisplayStyle.None;
            }
        }

        private VisualElement InitializeAttributeBox(
            VisualElement attributesPanel, out AttributeTab[] attributeTabs, params BaseSoldierAttribute[] attributes)
        {
            //var UICtrlMng = UIControllerManager.Instance;
            VisualElement attributeBox = UITK.AddElement(attributesPanel, "attributeBox");
            var buttonBox = UITK.AddElement(attributeBox, "buttonBox");

            // Creating Tabs
            attributeTabs = new AttributeTab[attributes.Length];
            for (int i = 0; i < attributeTabs.Length; i++)
                attributeTabs[i] = new(attributes[i], attributeBox, buttonBox);

            // Tab switching
            for (int i = 0; i < attributeTabs.Length; i++)
            {
                attributeTabs[i].attributeButton.clicked += attributeTabs[i].Activate;
                for (int j = 0; j < attributeTabs.Length; j++)
                {
                    if (i == j) continue;

                    attributeTabs[i].attributeButton.clicked += attributeTabs[j].Deactivate;
                }
            }

            return attributeBox;
        }
    }

    private class AttributeTab
    {
        public UIControllerManager UICtrlMng = UIControllerManager.Instance;
        public Color mainColor;
        public BaseSoldierAttribute attribute;
        public VisualElement attributeBox;
        public VisualElement buttonBox;

        public Button attributeButton;
        public Label attributeDescription;
        public VisualElement miscBox;
        public bool isActive = true;


        public AttributeTab(BaseSoldierAttribute attribute, VisualElement attributeBox, VisualElement buttonBox)
        {
            this.attribute = attribute;
            this.attributeBox = attributeBox;
            this.buttonBox = buttonBox;
            InitializeTab();
        }

        public void InitializeTab()
        {
            attribute.GetAttributeLobbyBox(
                out attributeButton, out attributeDescription, out miscBox);

            mainColor = attributeButton.style.backgroundColor.value;

            buttonBox.Add(attributeButton);

            attributeBox.Add(attributeDescription);
            attributeBox.Add(miscBox);
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            attributeButton.style.backgroundColor = mainColor;
            attributeButton.style.color = UICtrlMng.colorScheme.white;

            attributeDescription.style.display = DisplayStyle.Flex;
            miscBox.style.display = DisplayStyle.Flex;
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;

            attributeButton.style.backgroundColor = UICtrlMng.colorScheme.gray;
            attributeButton.style.color = mainColor;

            attributeDescription.style.display = DisplayStyle.None;
            miscBox.style.display = DisplayStyle.None;
        }
    }


    private void Awake()
    {
        InitializeUI();
    }
}