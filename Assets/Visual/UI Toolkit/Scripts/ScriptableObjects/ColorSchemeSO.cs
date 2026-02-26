using UnityEngine;

[CreateAssetMenu(fileName = "ColorSchemeSO", menuName = "Scriptable Objects/UI/ColorSchemeSO")]
public class ColorSchemeSO : ScriptableObject
{
    [Header("Neutral Colors")]
    public Color white;
    public Color lightGray;
    public Color gray;
    public Color darkGray;
    public Color Black;

    [Header("Accent Colors")]
    public Color accentCyan;
    public Color accentPurple;
    public Color accentRed;

    [Header("Base Colors")]
    public Color baseCyan;
    public Color basePurple;
    public Color baseRed;

    [Header("Module Colors")]
    public Color WPOrange;
    public Color WPOrangeBG;
    public Color TSBlue;
    public Color TSBlueBG;
    public Color DPGreen;
    public Color DPGreenBG;
}
