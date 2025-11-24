using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheetsSO defaultStyleSheet;
    [SerializeField] private StyleSheet[] styleSheets;

    private VisualElement unitPanel;
    private VisualElement[] unitProfiles = new VisualElement[4];


    private void Awake()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        unitPanel = UITK.AddElement(root, "unitPanel", "MainText");
        unitPanel.style.height = 250;
        unitPanel.style.top = 830;

        for (int i = 0; i < unitProfiles.Length; i++)
        {
            unitProfiles[i] = AddUnitProfile(unitPanel);
        }
    }

    private VisualElement AddUnitProfile(VisualElement unitPanel)
    {
        var unitProfile = UITK.AddElement(unitPanel, "unitProfile");

        var unitIcon = UITK.AddElement(unitProfile, "unitIcon");

        var skillBarsBox = UITK.AddElement(unitProfile, "skillBarsBox");

        var skillButton = UITK.AddElement<Button>(skillBarsBox, "skillButton");

        var healthBar = UITK.AddElement<ProgressBar>(skillBarsBox, "healthBar");

        var staminaBar = UITK.AddElement<ProgressBar>(skillBarsBox, "staminaBar");

        var infoPanel = UITK.AddElement(unitProfile, "infoPanel");


        var weaponDetails = UITK.AddElement(infoPanel, "weaponDetails", "infoBlock");

        var skillDetails = UITK.AddElement(infoPanel, "skillDetails", "infoBlock");

        //events
        unitProfile.RegisterCallback<PointerEnterEvent>(evt =>
        {
            infoPanel.style.height = 240;
        });

        unitProfile.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            infoPanel.style.height = 0;
        });

        return unitProfile; 
    }
}
