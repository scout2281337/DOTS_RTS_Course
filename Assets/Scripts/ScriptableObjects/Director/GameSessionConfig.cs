using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSessionConfig", menuName = "Scriptable Objects/Director/Game Session Config")]
public class GameSessionConfig : ScriptableObject
{
    [Header("Session")]
    public string SessionName = "Operation";
    public bool AutoStart = true;
    [Min(0f)] public float StartDelay = 0f;
    public bool DefeatWhenSquadIsDead = true;

    [Header("Threat")]
    [Min(0f)] public float StartingThreat;
    [Min(0f)] public float MaxThreat = 100f;
    [Min(0f)] public float ThreatGainPerMinute = 3f;

    [Header("Flow")]
    public GameSessionTaskDefinition[] Tasks = Array.Empty<GameSessionTaskDefinition>();
    public GameSessionEventRule[] EventRules = Array.Empty<GameSessionEventRule>();
}

[Serializable]
public class GameSessionTaskDefinition
{
    [Header("Identity")]
    public string Id = "task";
    public string Title = "Objective";
    [TextArea] public string Description;

    [Header("Completion")]
    public GameSessionTaskType Type = GameSessionTaskType.Manual;
    public bool AutoAdvance = true;
    public bool CompleteSessionOnFinish;
    public string TargetAnchorId;
    public string ActivityId;
    [Min(0f)] public float Duration = 5f;
    [Min(0f)] public float Radius = 5f;
    [Min(1)] public int RequiredFriendlyUnits = 1;
    [Min(1)] public int RequiredKillCount = 1;
    public Faction KillTargetFaction = Faction.Zombie;

    [Header("Presentation")]
    public bool ReportOnStart = true;
    public WorldEventImportance Importance = WorldEventImportance.Medium;
    public WorldEventKnowledge Knowledge = WorldEventKnowledge.Exact;
    [Min(0.1f)] public float ReportDuration = 5f;
}

[Serializable]
public class GameSessionEventRule
{
    [Header("Identity")]
    public string Id = "event";
    public bool Enabled = true;
    public string Label = "SIGNAL";

    [Header("Trigger")]
    public GameSessionEventTiming Timing = GameSessionEventTiming.TaskStarted;
    public string TaskId;
    [Min(0f)] public float Delay;
    [Min(0f)] public float RepeatInterval = 30f;
    [Min(0)] public int MaxTriggers;

    [Header("Threat Gate")]
    [Min(0f)] public float MinThreat;
    [Min(0f)] public float MaxThreat;

    [Header("Action")]
    public GameSessionEventAction Action = GameSessionEventAction.ReportWorldEvent;
    public GameObject Prefab;
    public bool ParentSpawnedPrefabToManager;
    public float ThreatAmount = 10f;

    [Header("Position")]
    public GameSessionPositionMode PositionMode = GameSessionPositionMode.Anchor;
    public string AnchorId;
    [Min(0f)] public float RandomRadius = 8f;

    [Header("World Event")]
    public bool ReportWorldEvent = true;
    public WorldEventType WorldEventType = WorldEventType.ObjectiveUpdated;
    public WorldEventImportance Importance = WorldEventImportance.Medium;
    public WorldEventKnowledge Knowledge = WorldEventKnowledge.Exact;
    [Min(0f)] public float EventRadius = 8f;
    [Min(0.1f)] public float EventDuration = 4f;
}

public enum GameSessionState
{
    Inactive,
    Briefing,
    Playing,
    Extraction,
    Victory,
    Defeat
}

public enum GameSessionTaskType
{
    Manual,
    Briefing,
    WaitTime,
    ReachArea,
    SurviveTime,
    KillEnemies,
    ActivateGenerator,
    Extract,
    EscortPlatform
}

public enum GameSessionEventTiming
{
    SessionStarted,
    TaskStarted,
    TaskCompleted,
    TimedRepeat,
    ThreatReached,
    Manual
}

public enum GameSessionEventAction
{
    ReportWorldEvent,
    SpawnPrefab,
    ArtilleryStrike,
    ArtilleryBarrage,
    AddThreat,
    SetThreat,
    CompleteCurrentTask
}

public enum GameSessionPositionMode
{
    ManagerTransform,
    Anchor,
    RandomAroundAnchor,
    RandomInSessionArea,
    FriendlySquadCenter
}
