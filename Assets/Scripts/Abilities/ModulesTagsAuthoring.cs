using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ModulesTagsAuthoring : MonoBehaviour
{
    public bool Berserker;
    public bool MainCharacter;
    public bool Ricoshet;
    public bool AcidBullets;

    public bool EnergyVampire;
    public bool ExtraBattery;
    public bool DeafSound;

    public bool DoubleShell;
    public bool BloodySpeedUp;



    public class Baker : Baker<ModulesTagsAuthoring>
    {
        public override void Bake(ModulesTagsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ModulesTags
            {
                Berserker = authoring.Berserker,
                MainCharacter = authoring.MainCharacter,
                Ricoshet = authoring.Ricoshet,
                AcidBullets = authoring.AcidBullets,
                EnergyVampire = authoring.EnergyVampire,
                ExtraBattery = authoring.ExtraBattery,
                DeafSound = authoring.DeafSound,
                DoubleShell = authoring.DoubleShell,
                BloodySpeedUp = authoring.BloodySpeedUp,
            });
        }
    }
}

public struct ModulesTags : IComponentData
{
    public bool Berserker;
    public bool MainCharacter;
    public bool Ricoshet;
    public bool AcidBullets;

    public bool EnergyVampire;
    public bool ExtraBattery;
    public bool DeafSound;

    public bool DoubleShell;
    public bool BloodySpeedUp;
}

