using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UnitSelectorUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;

    private VisualElement _selectionBox;
    private bool _selectionBoxActive = false;

    private Vector2 _selectionStartMousePosition;

    private GameControls _gameControls;


    private void OnSelectUnitsButtonPerformed(InputAction.CallbackContext context) 
    {
        _selectionStartMousePosition = _gameControls.Player.CursorPosition.ReadValue<Vector2>();
        _selectionBox.style.display = DisplayStyle.Flex;
        _selectionBoxActive = true;
    }

    private void OnSelectUnitsButtonCanceled(InputAction.CallbackContext context)
    {
        _selectionBox.style.display = DisplayStyle.None;
        _selectionBoxActive = false;
    }

    private void BuildUnitSelector()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        root.Clear();

        ViewController _UIController = ViewController.Instance;
        foreach (StyleSheet sheet in _UIController.DefaultStyleSheet.BaseStyles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in _styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        var canvas = UITK.AddElement(root, "canvas", "MainText");
        canvas.style.height = new Length(100, LengthUnit.Percent);
        canvas.pickingMode = PickingMode.Ignore;

        _selectionBox = UITK.AddElement(canvas, "selectionBox");
        _selectionBox.style.display = DisplayStyle.None;
    }

    private void UpdateSelectionBox()
    {
        Vector2 selectionEndMousePosition = _gameControls.Player.CursorPosition.ReadValue<Vector2>();
        //Debug.Log(selectionEndMousePosition);

        Vector2 resolutionDelta = new(1920 / Screen.width, 1080 / Screen.height);
        //Debug.Log(resolutionDelta);

        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(_selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Min(_selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(_selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Max(_selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        _selectionBox.style.left = lowerLeftCorner.x * resolutionDelta.x;
        _selectionBox.style.bottom = lowerLeftCorner.y * resolutionDelta.y;
        _selectionBox.style.width = (upperRightCorner.x - lowerLeftCorner.x) * resolutionDelta.x;
        _selectionBox.style.height = (upperRightCorner.y - lowerLeftCorner.y) * resolutionDelta.y;
    }


    private void Awake()
    {
        _gameControls = new GameControls();
        BuildUnitSelector();
    }

    private void Update()
    {
        if (_selectionBoxActive)
        {
            UpdateSelectionBox();
        }
    }

    private void OnEnable()
    {
        _gameControls.Player.Enable();

        _gameControls.Player.SelectionInteraction.started += OnSelectUnitsButtonPerformed;
        _gameControls.Player.SelectionInteraction.canceled += OnSelectUnitsButtonCanceled;
    }

    private void OnDisable()
    {
        _gameControls.Player.Disable();

        _gameControls.Player.SelectionInteraction.started -= OnSelectUnitsButtonPerformed;
        _gameControls.Player.SelectionInteraction.canceled -= OnSelectUnitsButtonCanceled;
    }
}
