using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UnitSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheetsSO defaultStyleSheet;
    [SerializeField] private StyleSheet[] styleSheets;

    private VisualElement selectionBox;
    private bool selectionBoxActive = false;

    private Vector2 selectionStartMousePosition;

    private GameControls gameControls;


    private void Awake()
    {
        gameControls = new GameControls();
        InitializeUI();
    }

    private void OnEnable()
    {
        gameControls.Player.Enable();

        gameControls.Player.SelectionInteraction.started += OnSelectUnitsButtonPerformed;
        gameControls.Player.SelectionInteraction.canceled += OnSelectUnitsButtonCanceled;
    }

    private void OnDisable()
    {
        gameControls.Player.Disable();

        gameControls.Player.SelectionInteraction.started -= OnSelectUnitsButtonPerformed;
        gameControls.Player.SelectionInteraction.canceled -= OnSelectUnitsButtonCanceled;
    }

    private void OnSelectUnitsButtonPerformed(InputAction.CallbackContext context) 
    {
        selectionStartMousePosition = gameControls.Player.CursorPosition.ReadValue<Vector2>();
        selectionBox.style.display = DisplayStyle.Flex;
        selectionBoxActive = true;
    }

    private void OnSelectUnitsButtonCanceled(InputAction.CallbackContext context)
    {
        selectionBox.style.display = DisplayStyle.None;
        selectionBoxActive = false;
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
    }

    private void UpdateSelectionBox()
    {
        Vector2 selectionEndMousePosition = gameControls.Player.CursorPosition.ReadValue<Vector2>();
        //Debug.Log(selectionEndMousePosition);

        Vector2 resolutionDelta = new(1920 / Screen.width, 1080 / Screen.height);
        //Debug.Log(resolutionDelta);

        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        selectionBox.style.left = lowerLeftCorner.x * resolutionDelta.x;
        selectionBox.style.bottom = lowerLeftCorner.y * resolutionDelta.y;
        selectionBox.style.width = (upperRightCorner.x - lowerLeftCorner.x) * resolutionDelta.x;
        selectionBox.style.height = (upperRightCorner.y - lowerLeftCorner.y) * resolutionDelta.y;
    }
}
