using System.Collections.Generic;
using UnityEngine;

public class ModuleManager : Singleton<ModuleManager>
{
    public int waveNumberTester = 1;

    [SerializeField] private ModuleBaseSO[] tierIModules;
    [SerializeField] private ModuleBaseSO[] tierIIModules;
    [SerializeField] private ModuleBaseSO[] tierIIIModules;
    [SerializeField] private Vector3[] waveTierChanceTable;

    private Dictionary<UnitClass, List<ModuleBaseSO>> unitEquippedModules = new();


    public ModuleBaseSO GetRandomModuleForUnit(UnitClass unit)
    {
        var chanceTable = waveTierChanceTable[waveNumberTester - 1];

        //Weight adaptation
        float totalWeight = chanceTable.x + chanceTable.y + chanceTable.z;
        float randomPoint = Random.value * totalWeight;

        //Getting random tier 
        var chosenModuleTier = randomPoint < chanceTable.x ? tierIModules
            : randomPoint < chanceTable.x + chanceTable.y ? tierIIModules
            : tierIIIModules;

        ModuleBaseSO rndModule = null;

        for ( int i = 0; i < 100; i++)
        {
            rndModule = chosenModuleTier[Random.Range(0, chosenModuleTier.Length)];

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
    }
}
