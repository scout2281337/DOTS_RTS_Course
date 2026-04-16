using UnityEngine;

[CreateAssetMenu(fileName = "ColorSchemeSO", menuName = "Scriptable Objects/UI/ColorSchemeSO")]
public class ColorSchemeSO : ScriptableObject
{
    [Header("Neutral Colors")]
    public Color White;
    public Color LightGray;
    public Color Gray;
    public Color DarkGray;
    public Color Black;

    [Header("Accent Colors")]
    public Color AccentMain;
    public Color AccentEnemy;

    [Header("Module Colors")]
    public Color AccentWP;
    public Color BaseWP;
    public Color AccentTS;
    public Color BaseTS;
    public Color AccentDP;
    public Color BaseDP;
}
