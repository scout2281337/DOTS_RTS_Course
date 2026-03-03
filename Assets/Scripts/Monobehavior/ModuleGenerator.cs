using UnityEngine;

public class ModuleGenerator : Singleton<ModuleGenerator>
{
    [SerializeField] private ModuleBaseSO[] tierIModules;
    [SerializeField] private ModuleBaseSO[] tierIIModules;
    [SerializeField] private ModuleBaseSO[] tierIIIModules;
    [SerializeField] private Vector3[] waveTierChanceTable;

    public int waveNumberTester = 1;


    public ModuleBaseSO GetRandomModule()
    {
        var chanceTable = waveTierChanceTable[waveNumberTester - 1];

        //Weight adaptation
        float totalWeight = chanceTable.x 
            + chanceTable.y
            + chanceTable.z;
        float randomPoint = Random.value * totalWeight;

        //Getting random tier 
        var chosenModuleTier = randomPoint < chanceTable.x ? tierIModules
            : randomPoint < chanceTable.x + chanceTable.y ? tierIIModules
            : tierIIIModules;

        return chosenModuleTier[Random.Range(0, chosenModuleTier.Length)];
    }

}
