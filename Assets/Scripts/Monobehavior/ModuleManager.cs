using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ModuleManager : Singleton<ModuleManager>
{
    public int waveNumberTester = 1;

    [SerializeField] private ModuleBaseSO[] tierIModules;
    [SerializeField] private ModuleBaseSO[] tierIIModules;
    [SerializeField] private ModuleBaseSO[] tierIIIModules;
    [SerializeField] private Vector3[] waveTierChanceTable;

    private readonly Dictionary<UnitClass, List<ModuleBaseSO>> unitEquippedModules = new();

    public ModuleBaseSO GetRandomModuleForUnit(UnitClass unit)
    {
        var chanceTable = waveTierChanceTable[waveNumberTester - 1];

        float totalWeight = chanceTable.x + chanceTable.y + chanceTable.z;
        float randomPoint = UnityEngine.Random.value * totalWeight;

        var chosenModuleTier = randomPoint < chanceTable.x ? tierIModules
            : randomPoint < chanceTable.x + chanceTable.y ? tierIIModules
            : tierIIIModules;

        ModuleBaseSO rndModule = null;

        for (int i = 0; i < 100; i++)
        {
            rndModule = chosenModuleTier[UnityEngine.Random.Range(0, chosenModuleTier.Length)];

            if (!unitEquippedModules.TryGetValue(unit, out var moduleList)) break;
            if (!moduleList.Contains(rndModule)) break;
        }

        return rndModule;
    }

    public void AddNewModuleToDict(UnitClass unit, ModuleBaseSO module)
    {
        if (!unitEquippedModules.TryGetValue(unit, out var moduleList))
        {
            moduleList = new List<ModuleBaseSO>();
            unitEquippedModules.Add(unit, moduleList);
        }

        moduleList.Add(module);

        AbilityEventListener.Instance.InvokeNewModule(unit, module);
        ApplyModuleToUnit(unit, module);
    }

    public bool GiveModuleToUnit(UnitClass unitClass, ModuleEffectType effectType)
    {
        if (effectType == ModuleEffectType.None)
            return false;

        if (!TryResolveUnitEntity(unitClass, out var em, out var unitEntity))
            return false;

        ApplyModuleEffectToEntity(em, unitEntity, effectType);
        return true;
    }

    private void ApplyModuleToUnit(UnitClass unitClass, ModuleBaseSO module)
    {
        if (module == null)
            return;

        if (!TryResolveUnitEntity(unitClass, out var em, out var unitEntity))
            return;

        ModuleEffectType effect = ResolveModuleEffect(module);
        ApplyModuleEffectToEntity(em, unitEntity, effect);
    }

    private static bool TryResolveUnitEntity(UnitClass unitClass, out EntityManager em, out Entity unitEntity)
    {
        em = default;
        unitEntity = Entity.Null;

        if (FriendlyUnitManager.Instance == null)
            return false;

        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out unitEntity))
            return false;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return false;

        em = world.EntityManager;
        if (!em.Exists(unitEntity))
            return false;

        return true;
    }

    private static void ApplyModuleEffectToEntity(EntityManager em, Entity unitEntity, ModuleEffectType effect)
    {
        switch (effect)
        {
            case ModuleEffectType.Berserker:
                if (!em.HasComponent<BerserkerICD>(unitEntity))
                    em.AddComponentData(unitEntity, new BerserkerICD());
                break;

            case ModuleEffectType.Ricochet:
                if (!em.HasComponent<RicochetModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new RicochetModule
                    {
                        Cooldown = 3f,
                        CooldownLeft = 0f,
                        Radius = 5f,
                        DamageMultiplier = 2f
                    });
                }
                break;

            case ModuleEffectType.AcidBullets:
                if (!em.HasComponent<AcidBulletsModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new AcidBulletsModule
                    {
                        MaxStacks = 60,
                        MoveSlowPerStack = 0.005f,
                        DamageTakenPerStack = 0.005f
                    });
                }
                break;

            case ModuleEffectType.EnergyVampire:
                if (!em.HasComponent<EnergyVampireModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new EnergyVampireModule
                    {
                        CooldownReductionOnKill = 0.1f
                    });
                }
                break;

            case ModuleEffectType.ExtraBattery:
                if (!em.HasComponent<ExtraBatteryModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new ExtraBatteryModule
                    {
                        MaxCharges = 1,
                        Charges = 1
                    });
                }
                else
                {
                    var battery = em.GetComponentData<ExtraBatteryModule>(unitEntity);
                    battery.MaxCharges = Mathf.Max(1, battery.MaxCharges);
                    battery.Charges = battery.MaxCharges;
                    em.SetComponentData(unitEntity, battery);
                }
                break;

            case ModuleEffectType.DeafeningEcho:
                if (!em.HasComponent<DeafSoundModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new DeafSoundModule
                    {
                        StunDuration = 0.3f
                    });
                }
                break;

            case ModuleEffectType.Vampirism:
                if (!em.HasComponent<VampirismModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new VampirismModule
                    {
                        Radius = 3f,
                        HealAmount = 1f
                    });
                }
                break;

            case ModuleEffectType.BloodySpeedUp:
                if (!em.HasComponent<BloodySpeedUpModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new BloodySpeedUpModule
                    {
                        Stacks = 0,
                        MaxStacks = 30,
                        SpeedPerStack = 0.01f,
                        ResetTimer = 7f,
                        ResetTimerMax = 7f
                    });
                }
                break;

            case ModuleEffectType.SupplyLines:
                if (!em.HasComponent<SupplyLinesModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new SupplyLinesModule
                    {
                        RespawnTimeMultiplier = 0.7f
                    });
                }
                break;

            case ModuleEffectType.DoubleShell:
                if (!em.HasComponent<DoubleShell>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new DoubleShell
                    {
                        PercentToResist = 0.15f
                    });
                }
                break;
        }
    }

    private static ModuleEffectType ResolveModuleEffect(ModuleBaseSO module)
    {
        if (module.effectType != ModuleEffectType.None)
            return module.effectType;

        string key = $"{module.name} {module.description}".ToLowerInvariant();

        if (key.Contains("ricochet") || key.Contains("ricoshet") || key.Contains("rikoshet"))
            return ModuleEffectType.Ricochet;
        if (key.Contains("acid"))
            return ModuleEffectType.AcidBullets;
        if (key.Contains("energy vampire") || key.Contains("energyvampire"))
            return ModuleEffectType.EnergyVampire;
        if (key.Contains("battery"))
            return ModuleEffectType.ExtraBattery;
        if (key.Contains("echo") || key.Contains("deaf"))
            return ModuleEffectType.DeafeningEcho;
        if (key.Contains("vampirism"))
            return ModuleEffectType.Vampirism;
        if (key.Contains("bloody"))
            return ModuleEffectType.BloodySpeedUp;
        if (key.Contains("supply"))
            return ModuleEffectType.SupplyLines;
        if (key.Contains("berserk"))
            return ModuleEffectType.Berserker;
        if (key.Contains("shell"))
            return ModuleEffectType.DoubleShell;

        return ModuleEffectType.None;
    }
}
