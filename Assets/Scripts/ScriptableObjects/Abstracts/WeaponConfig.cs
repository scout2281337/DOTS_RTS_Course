using UnityEngine;
using UnityEngine.UIElements;

public abstract class WeaponConfig : BaseSoldierAttribute, IInfoBlockUI
{
    public float damage;
    public float fireRate;
    public float range;
    public WeaponType weaponType;
    public float typeStat;
    public float explosiveRange;
    public int maxPierceCount;


    //if can charged attack
    public float ChargedAttackDamage;
    public float ChargedAttackRange;

    public VisualElement SetInfoBlockUI(VisualElement infoPanel, ColorSchemeSO colorScheme)
    {
        var weaponDetails = UITK.AddElement(infoPanel, "weaponDetails", "infoBlock");
        weaponDetails.style.color = colorScheme.white;

            var damageRow = UITK.AddElement(weaponDetails, "damageRow", "detailRow");

                var damageText = UITK.AddElement<Label>(damageRow, "damageText", "detailText");
                damageText.text = "Damage:";

                var damageAmount = UITK.AddElement<Label>(damageRow, "damageAmount", "detailAmount");
                //damageAmount.style.color = colorScheme.baseRed;
                damageAmount.text = damage.ToString();

            var fireRateRow = UITK.AddElement(weaponDetails, "fireRateRow", "detailRow");

                var fireRateText = UITK.AddElement<Label>(fireRateRow, "fireRateText", "detailText");
                fireRateText.text = "Fire Rate:";

                var fireRateAmount = UITK.AddElement<Label>(fireRateRow, "fireRateAmount", "detailAmount");
                //fireRateAmount.style.color = colorScheme.baseRed;
                fireRateAmount.text = fireRate.ToString();

            var rangeRow = UITK.AddElement(weaponDetails, "rangeRow", "detailRow");

                var rangeText = UITK.AddElement<Label>(rangeRow, "rangeText", "detailText");
                rangeText.text = "Radius:";

                var rangeAmount = UITK.AddElement<Label>(rangeRow, "rangeAmount", "detailAmount");
                //rangeAmount.style.color = colorScheme.baseRed;
                rangeAmount.text = range.ToString();

        return weaponDetails;
    }

    public override void GetAttributeLobbyBox(Color BG, Color font, out Button attributeButton, out Label attributeDescription, out VisualElement miscBox)
    {
        base.GetAttributeLobbyBox(BG, font, out attributeButton, out attributeDescription, out miscBox);

        var damageLabel = UITK.AddElement<Label>(miscBox, "damageLabel");
        damageLabel.style.color = font;
        damageLabel.text = "Урон: " + damage;

        var fireRateLabel = UITK.AddElement<Label>(miscBox, "fireRateLabel");
        fireRateLabel.style.color = font;
        fireRateLabel.text = "Скорострельность: " + fireRate;

        var rangeLabel = UITK.AddElement<Label>(miscBox, "rangeLabel");
        rangeLabel.style.color = font;
        rangeLabel.text = "Дальность: " + range;

        string weaponTypeName = weaponType switch
        {
            WeaponType.Explosive => "/Разрывное/ Радиус: ",
            WeaponType.AntiMaterial => "/Антиматериальное/ Пробитие: ",
            WeaponType.Dispersive => "/Дисперсное/ Радиус: ",
            _ => "Error"
        };

        var weaponTypeLabel = UITK.AddElement<Label>(miscBox, "weaponTypeLabel");
        weaponTypeLabel.style.color = font;
        weaponTypeLabel.text = weaponTypeName + typeStat;
    }
}
