using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ViewController UIController;

    [SerializeField] private SoldierAttributeGroupConfig[] attributeGroups;

    private VisualElement lobby;


    private void BuildLobby()
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

        lobby = UITK.AddElement(root, "lobby");

        var topSection = UITK.AddElement(lobby, "topSection");
        BuildTop(topSection);

        var midSection = UITK.AddElement(lobby, "midSection");
        BuildMiddle(midSection, out Button[] soldierIcons);

        var bottomSection = UITK.AddElement(lobby, "bottomSection");
        BuildBottom(bottomSection, out AttributesContainer[] attributesContainers);


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
    }

    private void BuildTop(VisualElement topSection)
    {
        var startButton = UITK.AddElement<Button>(topSection, "PrimaryButton", "H2", "startButton");
        startButton.text = "ВЫСАДКА";
        startButton.clicked += () => {
            SceneManager.LoadScene("SampleScene");
        };

        var backButton = UITK.AddElement<Button>(topSection, "TertiaryButton", "P1", "backButton");
        backButton.text = "Назад";
        backButton.clicked += () => {
            StartCoroutine(CameraMotion.MoveCameraToPoint(new(-3.5f, 2.6f, -0.4f), 1.2f));
        };
    }

    private void BuildMiddle(VisualElement midSection, out Button[] soldierIcons)
    {
        // TO DO rework with configs 
        var difficultyPanel = UITK.AddElement(midSection, "LobbyPanel", "difficultyPanel");

        var difficultyButtonBox = UITK.AddElement(difficultyPanel, "difficultyButtonBox");

        for (int i = 0; i < 3; i++)
        {
            var difficultyButton = UITK.AddElement<Button>(difficultyButtonBox, "difficultyButton");
        }

        var difficultyDescriptionBox = UITK.AddElement(difficultyPanel, "difficultyDescriptionBox");

        var difficultyName = UITK.AddElement<Label>(difficultyDescriptionBox, "P1", "difficultyName");
        difficultyName.text = "ЛЕГКО";

        var difficultyModifiers = UITK.AddElement<Label>(difficultyDescriptionBox, "P3", "difficultyModifiers");
        difficultyModifiers.text = "Модификатор: +100% \nМодификатор: +100% \nМодификатор: +100% \nМодификатор: +100%";

        // TeamPanel setup
        var teamPanel = UITK.AddElement(midSection, "teamPanel");

        for (int i = 0; i < 4; i++)
        {
            var memberField = UITK.AddElement<Button>(teamPanel, "InvisibleButton", "memberField");
        }
        
        // SoldiersPanel setup
        var soldiersPanel = UITK.AddElement<ScrollView>(midSection, "LobbyPanel", "soldiersPanel");

        // Columns for displaying icons 
        VisualElement[] iconColumns = new VisualElement[2];
        for (int i = 0; i < 2; i++)
            iconColumns[i] = UITK.AddElement(soldiersPanel, "iconColumn");

        // Creating buttons and layout
        soldierIcons = new Button[attributeGroups.Length];
        for (int i = 0; i < soldierIcons.Length; i++)
        {
            // Placing icons in right column
            var column = i % 2 == 0 ? iconColumns[0] : iconColumns[1];

            soldierIcons[i] = UITK.AddElement<Button>(column, "RigidButton", "soldierIcon");
            soldierIcons[i].style.backgroundImage = attributeGroups[i].icon;
        }
    }

    private void BuildBottom(VisualElement bottomSection, out AttributesContainer[] attributesContainers)
    {
        var attributesPanel = UITK.AddElement(bottomSection, "LobbyPanel", "attributesPanel");

        // Creating containers and hiding them, so that there is no overlap between each over
        attributesContainers = new AttributesContainer[attributeGroups.Length];
        for (int i = 0; i < attributeGroups.Length; i++)
        {
            attributesContainers[i] = new AttributesContainer(attributeGroups[i], attributesPanel);
            attributesContainers[i].Deactivate();
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

            bodyAttributeBox = BuildAttributeBox(attributesPanel, out bodyAttributeTabs, attributeGroup.bodyConfig);
            weaponAttributeBox = BuildAttributeBox(attributesPanel, out weaponAttributeTabs, attributeGroup.weaponConfigs);
            skillAttributeBox = BuildAttributeBox(attributesPanel, out skillAttributeTabs, attributeGroup.skillConfigs);
        }

        private VisualElement BuildAttributeBox(
            VisualElement attributesPanel, out AttributeTab[] attributeTabs, params BaseSoldierAttribute[] attributes)
        {
            VisualElement attributeBox = UITK.AddElement(attributesPanel, "attributeBox");
            var buttonBox = UITK.AddElement(attributeBox, "buttonBox");

            // Creating Tabs
            attributeTabs = new AttributeTab[attributes.Length];
            for (int i = 0; i < attributeTabs.Length; i++)
                attributeTabs[i] = new(attributes[i], attributeBox, buttonBox);

            // Disabling all tabs except first one, so that they do not overlap
            for (int i = 1; i < attributeTabs.Length; i++)
                attributeTabs[i].Deactivate();

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

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            VisualElement[] boxes = { bodyAttributeBox, weaponAttributeBox, skillAttributeBox };
            foreach (var box in boxes)
            {
                box.style.display = DisplayStyle.Flex;
            }
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;

            VisualElement[] boxes = { bodyAttributeBox, weaponAttributeBox, skillAttributeBox };
            foreach (var box in boxes)
            {
                box.style.display = DisplayStyle.None;
            }
        }
    }

    private class AttributeTab
    {
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

            BuildTab();
        }

        public void BuildTab()
        {
            attribute.GetAttributeLobbyBox(
                out attributeButton, out attributeDescription, out miscBox);

            mainColor = attributeButton.style.backgroundColor.value;

            buttonBox.Add(attributeButton);
            attributeBox.Add(miscBox);
            attributeBox.Add(attributeDescription);

            attributeButton.AddToClassList("DeactivatedButton");
            attributeButton.EnableInClassList("DeactivatedButton", false);
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            attributeButton.EnableInClassList("DeactivatedButton", false);

            attributeDescription.style.display = DisplayStyle.Flex;
            miscBox.style.display = DisplayStyle.Flex;
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;

            attributeButton.EnableInClassList("DeactivatedButton", true);

            attributeDescription.style.display = DisplayStyle.None;
            miscBox.style.display = DisplayStyle.None;
        }
    }


    private void Awake()
    {
        BuildLobby();
    }

    private void Update()
    {
        UITK.TrackUIToWorldPosition(transform.position, lobby, Camera.main, new Vector2(0, 0));
    }
}