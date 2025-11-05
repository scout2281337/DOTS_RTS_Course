using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UnitsUIController : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheetsSO styleSheet;

    private VisualElement selectionBox;
    private bool selectionBoxActive = false;

    private Vector2 selectionStartMousePosition;

    private bool isPresedUnitsSelectionButton;
    private GameControls gameControls;
    private void Awake()
    {
        InitializeUI();
        gameControls = new GameControls();
    }

    private void OnEnable()
    {
        gameControls.Player.Enable();

        gameControls.Player.SelectUnitsButton.started += OnSelectUnitsButtonPerformed;
        gameControls.Player.SelectUnitsButton.canceled += OnSelectUnitsButtonCanceled;
    }

    private void OnDisable()
    {
        gameControls.Player.Disable();

        gameControls.Player.SelectUnitsButton.started -= OnSelectUnitsButtonPerformed;
        gameControls.Player.SelectUnitsButton.canceled -= OnSelectUnitsButtonCanceled;
    }

    private void OnSelectUnitsButtonPerformed(InputAction.CallbackContext context) 
    {
        isPresedUnitsSelectionButton = true;

        //Debug.Log("сработало");
        selectionStartMousePosition = gameControls.Player.SelectUnitsButtonPosition.ReadValue<Vector2>();//Input.mousePosition;
        selectionBox.style.display = DisplayStyle.Flex;
        selectionBoxActive = true;
    }

    private void OnSelectUnitsButtonCanceled(InputAction.CallbackContext context)
    {
        isPresedUnitsSelectionButton = false;
        //Debug.Log("Released!");
    }
    private void InitializeUI()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in styleSheet.styles)
            root.styleSheets.Add(sheet);

        var canvas = UITK.AddElement(root, "canvas", "MainText");
        canvas.style.height = new Length(100, LengthUnit.Percent);
        canvas.pickingMode = PickingMode.Ignore;

        selectionBox = UITK.AddElement(canvas, "selectionBox");
        selectionBox.style.display = DisplayStyle.None;
    }

    private void Update()
    {

        if (selectionBoxActive)
        {
            UpdateSelectionBox();
        }

        if  (!isPresedUnitsSelectionButton /*Input.GetMouseButtonUp(0)*/)
        {
            //Debug.Log("сработало dsrk.xtybt");
            selectionBox.style.display = DisplayStyle.None;
            selectionBoxActive = false;
        }
    }

    private void UpdateSelectionBox()
    {
        Vector2 selectionEndMousePosition = gameControls.Player.SelectUnitsButtonPosition.ReadValue<Vector2>();

        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        selectionBox.style.left = lowerLeftCorner.x;
        selectionBox.style.bottom = lowerLeftCorner.y;
        selectionBox.style.width = upperRightCorner.x - lowerLeftCorner.x;
        selectionBox.style.height = upperRightCorner.y - lowerLeftCorner.y;
    }
}
