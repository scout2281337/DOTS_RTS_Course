using UnityEngine;
using UnityEngine.UIElements;

public class UnitsUIController : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheetsSO styleSheet;

    private VisualElement selectionBox;
    private bool selectionBoxActive = false;

    private Vector2 selectionStartMousePosition;


    private void Awake()
    {
        InitializeUI();

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
        if (Input.GetMouseButtonDown(0))
        {
            selectionStartMousePosition = Input.mousePosition;
            selectionBox.style.display = DisplayStyle.Flex;
            selectionBoxActive = true;
        }

        if (selectionBoxActive)
        {
            UpdateSelectionBox();
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectionBox.style.display = DisplayStyle.None;
            selectionBoxActive = false;
        }
    }

    private void UpdateSelectionBox()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;

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
