using UnityEngine;

public class WorldEventReporter : MonoBehaviour
{
    [SerializeField] private WorldEventType type = WorldEventType.Custom;
    [SerializeField] private WorldEventImportance importance = WorldEventImportance.Medium;
    [SerializeField] private WorldEventKnowledge knowledge = WorldEventKnowledge.Exact;
    [SerializeField] private string label = "SIGNAL";
    [SerializeField] private float radius = 8f;
    [SerializeField] private float duration = 4f;
    [SerializeField] private bool reportOnStart;

    private void Start()
    {
        if (reportOnStart)
            Report();
    }

    public void Report()
    {
        WorldEventUtility.Report(type, transform.position, importance, knowledge, radius, duration, label);
    }
}
