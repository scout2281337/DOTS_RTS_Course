using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ModuleManager : Singleton<ModuleManager>
{
    public int WaveNumberTester = 1;

    [SerializeField] private ModuleBaseSO[] _tierIModules;
    [SerializeField] private ModuleBaseSO[] _tierIIModules;
    [SerializeField] private ModuleBaseSO[] _tierIIIModules;
    [SerializeField] private Vector3[] _waveTierChanceTable;

    private readonly Dictionary<UnitClass, List<ModuleBaseSO>> _unitEquippedModules = new();

    public ModuleBaseSO GetRandomModuleForUnit(UnitClass unit)
    {
        var chanceTable = _waveTierChanceTable[WaveNumberTester - 1];

        float totalWeight = chanceTable.x + chanceTable.y + chanceTable.z;
        float randomPoint = Random.value * totalWeight;

        var chosenModuleTier = randomPoint < chanceTable.x ? _tierIModules
            : randomPoint < chanceTable.x + chanceTable.y ? _tierIIModules
            : _tierIIIModules;

        ModuleBaseSO rndModule = null;

        for (int i = 0; i < 100; i++)
        {
            rndModule = chosenModuleTier[Random.Range(0, chosenModuleTier.Length)];

            if (!_unitEquippedModules.TryGetValue(unit, out var moduleList)) break;
            if (!moduleList.Contains(rndModule)) break;
        }

        return rndModule;
    }

    public void AddNewModuleToDict(UnitClass unit, ModuleBaseSO module)
    {
        if (!_unitEquippedModules.TryGetValue(unit, out var moduleList))
        {
            moduleList = new List<ModuleBaseSO>();
            _unitEquippedModules.Add(unit, moduleList);
        }

        moduleList.Add(module);

        EventMediator.Instance.InvokeNewModule(unit, module);
        ApplyModuleToUnit(unit, module);
    }

    public bool GiveModuleToUnit(UnitClass unitClass, Module effectType)
    {
        if (effectType == Module.None)
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

        Module effect = ResolveModuleEffect(module);
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

    private static void ApplyModuleEffectToEntity(EntityManager em, Entity unitEntity, Module effect)
    {
        switch (effect)
        {
            case Module.Berserker:
                if (!em.HasComponent<BerserkerICD>(unitEntity))
                    em.AddComponentData(unitEntity, new BerserkerICD());
                break;

            case Module.Ricochet:
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

            case Module.AcidBullets:
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

            case Module.EnergyVampire:
                if (!em.HasComponent<EnergyVampireModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new EnergyVampireModule
                    {
                        CooldownReductionOnKill = 0.1f
                    });
                }
                break;

            case Module.ExtraBattery:
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

            case Module.DeafeningReverb:
                if (!em.HasComponent<DeafSoundModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new DeafSoundModule
                    {
                        StunDuration = 0.3f
                    });
                }
                break;

            case Module.Vampirism:
                if (!em.HasComponent<VampirismModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new VampirismModule
                    {
                        Radius = 3f,
                        HealAmount = 1f
                    });
                }
                break;

            case Module.BloodySpeedUp:
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

            case Module.SupplyLines:
                if (!em.HasComponent<SupplyLinesModule>(unitEntity))
                {
                    em.AddComponentData(unitEntity, new SupplyLinesModule
                    {
                        RespawnTimeMultiplier = 0.7f
                    });
                }
                break;

            case Module.DoubleShell:
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

    private static Module ResolveModuleEffect(ModuleBaseSO module)
    {
        if (module.module != Module.None)
            return module.module;

        string key = $"{module.name} {module.description}".ToLowerInvariant();

        if (key.Contains("ricochet") || key.Contains("ricoshet") || key.Contains("rikoshet"))
            return Module.Ricochet;
        if (key.Contains("acid"))
            return Module.AcidBullets;
        if (key.Contains("energy vampire") || key.Contains("energyvampire"))
            return Module.EnergyVampire;
        if (key.Contains("battery"))
            return Module.ExtraBattery;
        if (key.Contains("echo") || key.Contains("deaf"))
            return Module.DeafeningReverb;
        if (key.Contains("vampirism"))
            return Module.Vampirism;
        if (key.Contains("bloody"))
            return Module.BloodySpeedUp;
        if (key.Contains("supply"))
            return Module.SupplyLines;
        if (key.Contains("berserk"))
            return Module.Berserker;
        if (key.Contains("shell"))
            return Module.DoubleShell;

        return Module.None;
    }
}
