using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Scriptable Objects/Classes/WeaponConfig")]
public class WeaponConfig : ScriptableObject, IInfoBlockUI
{
    public float damage;
    public float fireRate;
    public float range;
    public WeaponTypes weaponType;
    public float typeStat;

    public VisualElement SetInfoBlockUI(VisualElement infoPanel)
    {
        var weaponDetails = UITK.AddElement(infoPanel, "weaponDetails", "infoBlock");

            var damageRow = UITK.AddElement(weaponDetails, "damageRow", "detailRow");

                var damageText = UITK.AddElement<Label>(damageRow, "damageText", "detailText");
                damageText.text = "Damage:";

                var damageAmount = UITK.AddElement<Label>(damageRow, "damageAmount", "detailAmount");
                damageAmount.text = damage.ToString();

            var fireRateRow = UITK.AddElement(weaponDetails, "fireRateRow", "detailRow");

                var fireRateText = UITK.AddElement<Label>(fireRateRow, "fireRateText", "detailText");
                fireRateText.text = "Fire Rate:";

                var fireRateAmount = UITK.AddElement<Label>(fireRateRow, "fireRateAmount", "detailAmount");
                fireRateAmount.text = fireRate.ToString();

            var rangeRow = UITK.AddElement(weaponDetails, "rangeRow", "detailRow");

                var rangeText = UITK.AddElement<Label>(rangeRow, "rangeText", "detailText");
                rangeText.text = "Range:";

                var rangeAmount = UITK.AddElement<Label>(rangeRow, "rangeAmount", "detailAmount");
                rangeAmount.text = range.ToString();



        return weaponDetails;
    }
}
