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

    public VisualElement SetInfoBlockUI(VisualElement infoPanel)
    {
        var weaponDetails = UITK.AddElement(infoPanel, "weaponDetails", "infoBlock");

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

    public override void GetAttributeLobbyBox(out Button attributeButton, out Label attributeDescription, out VisualElement statsBox)
    {
        base.GetAttributeLobbyBox(out attributeButton, out attributeDescription, out statsBox);

        attributeButton.AddToClassList("WP");


        statsBox.AddToClassList("WP");

        var weaponTypeLabel = UITK.AddElement<Label>(statsBox, "P1", "weaponTypeLabel");
        weaponTypeLabel.text = weaponType switch
        {
            WeaponType.Explosive => "Разрывное",
            WeaponType.AntiMaterial => "Антиматериальное",
            WeaponType.Dispersive => "Дисперсное",
            _ => "Error"
        }; ;

        var horizontalPair = UITK.AddElement(statsBox, "horizontalPair");

        var damageFireRateColumn = UITK.AddElement(horizontalPair, "statsColumn");

        var damageLabel = UITK.AddElement<Label>(damageFireRateColumn, "damageLabel");
        damageLabel.text = "Урон: " + damage;

        var fireRateLabel = UITK.AddElement<Label>(damageFireRateColumn, "fireRateLabel");
        fireRateLabel.text = "Скорострельность: " + fireRate;

        var rangeWeaponTypeColumn = UITK.AddElement(horizontalPair, "statsColumn");

        var rangeLabel = UITK.AddElement<Label>(rangeWeaponTypeColumn, "rangeLabel");
        rangeLabel.text = "Дальность: " + range;

        string weaponTypeStatName = weaponType switch
        {
            WeaponType.Explosive => "Радиус: ",
            WeaponType.AntiMaterial => "Пробитие: ",
            WeaponType.Dispersive => "Радиус: ",
            _ => "Error"
        };
        var weaponTypeStatLabel = UITK.AddElement<Label>(rangeWeaponTypeColumn, "weaponTypeStatLabel");
        weaponTypeStatLabel.text = weaponTypeStatName + typeStat;


        attributeDescription.AddToClassList("WP");
    }
}
