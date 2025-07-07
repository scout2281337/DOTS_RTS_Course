using System;
using UnityEngine;

public class UnitSelectionManagerUI : MonoBehaviour
{
    [SerializeField] private RectTransform selectionAreaRectTransform;
    [SerializeField] private Canvas canvas;

    private void Start()
    {
        UnitSelectionManager.Instance.OnSelectionAreaStart += UnitSelectionManager_OnSelectionAreaStart;
        UnitSelectionManager.Instance.OnSelectionAreaEnd += UnitSelectionManager_OnSelectionAreaEnd;
        
        selectionAreaRectTransform.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (selectionAreaRectTransform.gameObject.activeSelf) 
        {
            UpdateVisual();
        }    
    }
    private void UnitSelectionManager_OnSelectionAreaStart(object sender, EventArgs e)
    {
        selectionAreaRectTransform.gameObject.SetActive(true);

        UpdateVisual();
    }

    private void UnitSelectionManager_OnSelectionAreaEnd(object sender, EventArgs e)
    {
        selectionAreaRectTransform.gameObject.SetActive(false);
    }

    private void UpdateVisual() 
    {
        Rect selectionareaRect = UnitSelectionManager.Instance.GetSelectionareaRect();

        float canvasScale = canvas.transform.localScale.x;
        selectionAreaRectTransform.anchoredPosition = new Vector2(selectionareaRect.x, selectionareaRect.y) / canvasScale; 
        selectionAreaRectTransform.sizeDelta = new Vector2(selectionareaRect.width, selectionareaRect.height) / canvasScale;
    }
}
