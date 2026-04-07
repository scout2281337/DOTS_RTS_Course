using System;
using UnityEngine;

public class UIControllerMediator : MonoBehaviour
{
    public StyleSheetsSO defaultStyleSheet;
    public BaseUITextures baseTextures;
    //public ColorSchemeSO colorScheme;

    public UnitSelectorUI unitSelectorUI;
    public UnitPanelUI unitPanelUI;
    public ModuleSelectorUI moduleSelectorUI;

    public Action escBuffer;
}
