using UnityEngine;
using UnityEngine.UIElements;

public class UnitPanelUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheetsSO defaultStyleSheet;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ColorSchemeSO colorScheme;
    [SerializeField] private Texture2D statsBoardBG;

    [SerializeField] private SkillIconsSO skillIcons;

    private VisualElement unitPanel;
    private VisualElement[] unitProfiles = new VisualElement[4];

    public ClassConfig classInfoBlockTester;
    public WeaponConfig weaponInfoBlockTester;
    public SkillConfig skillInfoBlockTester;


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
        unitProfile.style.backgroundColor = colorScheme.Black;
         
            var skillButton = UITK.AddElement<Button>(unitProfile, "skillButton");
            skillButton.style.backgroundImage = skillIcons.icons[0];
            
            var barsBox = UITK.AddElement(unitProfile, "barsBox");
                     
                var healthBar = UITK.AddElement<ProgressBar>(barsBox, "healthBar");
                
                var staminaBar = UITK.AddElement<ProgressBar>(barsBox, "staminaBar");
                
            var infoPanel = UITK.AddElement(unitProfile, "infoPanel");
            infoPanel.style.backgroundColor = colorScheme.darkGray;

                var statsBoard = UITK.AddElement(infoPanel, "statsBoard", "H2");
                statsBoard.style.backgroundColor = colorScheme.gray;
                statsBoard.style.backgroundImage = statsBoardBG;

                    var weaponBoard = UITK.AddElement(statsBoard, "statBoard");
                    weaponBoard.style.color = colorScheme.white;

                        var DMG = UITK.AddElement<Label>(weaponBoard, "DMG", "statElement");
                        DMG.text = weaponInfoBlockTester.damage.ToString();

                        var FIRE = UITK.AddElement<Label>(weaponBoard, "FIRE", "statElement");
                        FIRE.text = weaponInfoBlockTester.fireRate.ToString();

                        var DST = UITK.AddElement<Label>(weaponBoard, "DST", "statElement");
                        DST.text = weaponInfoBlockTester.distance.ToString();

                    var skillBoard = UITK.AddElement(statsBoard, "statBoard");
                    skillBoard.style.color = colorScheme.white;

                        var PWR = UITK.AddElement<Label>(skillBoard, "PWR", "statElement");
                        PWR.text = skillInfoBlockTester.power.ToString();

                        var TIME = UITK.AddElement<Label>(skillBoard, "TIME", "statElement");
                        TIME.text = skillInfoBlockTester.duration.ToString() + "/" 
                                    + skillInfoBlockTester.cooldown.ToString();

                        var AREA = UITK.AddElement<Label>(skillBoard, "AREA", "statElement");
                        AREA.text = skillInfoBlockTester.area.ToString() + "/"
                                    + skillInfoBlockTester.range.ToString();

        //var classDetails = classInfoBlockTester.SetInfoBlockUI(infoPanel, colorScheme);

        //var weaponDetails = weaponInfoBlockTester.SetInfoBlockUI(infoPanel, colorScheme);

        //var skillDetails = skillInfoBlockTester.SetInfoBlockUI(infoPanel, colorScheme);

        return unitProfile;
    }


    private void Awake()
    {
        InitializeUI();
    }
}
