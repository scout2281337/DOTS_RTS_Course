using Unity.Collections;
using Unity.Entities;
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

    public ClassConfig classInfoBlockTester;
    public WeaponConfig weaponInfoBlockTester;
    public SkillConfig skillInfoBlockTester;

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

        unitPanel = UITK.AddElement(root, "unitPanel", "P");
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

            var skillButton = UITK.AddElement<Button>(unitProfile, "skillButton");

            var barsBox = UITK.AddElement(unitProfile, "barsBox");
            
                var healthBar = UITK.AddElement<ProgressBar>(barsBox, "healthBar");

                var staminaBar = UITK.AddElement<ProgressBar>(barsBox, "staminaBar");
            
            var infoPanel = UITK.AddElement(unitProfile, "infoPanel");
            
                var classDetails = classInfoBlockTester.SetInfoBlockUI(infoPanel);

                var weaponDetails = weaponInfoBlockTester.SetInfoBlockUI(infoPanel);

                var skillDetails = skillInfoBlockTester.SetInfoBlockUI(infoPanel);


        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<AnabolikStimulator>().Build(entityManager);
            
            

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
