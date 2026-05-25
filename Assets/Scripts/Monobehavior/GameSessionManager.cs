using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class GameSessionManager : Singleton<GameSessionManager>
{
    [Serializable]
    public struct SessionAnchor
    {
        public string Id;
        public Transform Transform;
    }

    private class EventRuleRuntime
    {
        public GameSessionEventRule Rule;
        public int TriggerCount;
        public float NextTriggerTime;
        public bool WasBelowThreatGate = true;
    }

    private struct ScheduledEvent
    {
        public EventRuleRuntime Runtime;
        public float FireTime;
    }

    [Header("Config")]
    [SerializeField] private GameSessionConfig config;

    [Header("Scene Anchors")]
    [SerializeField] private SessionAnchor[] anchors = Array.Empty<SessionAnchor>();

    [Header("Random Area")]
    [SerializeField] private Vector3 sessionAreaCenter = Vector3.zero;
    [SerializeField] private Vector2 sessionAreaSize = new(100f, 100f);

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    private readonly List<EventRuleRuntime> eventRuntimes = new();
    private readonly List<ScheduledEvent> scheduledEvents = new();

    private GameSessionTaskDefinition activeTask;
    private GameSessionState state = GameSessionState.Inactive;
    private float sessionTimer;
    private float taskTimer;
    private float threatLevel;
    private int activeTaskIndex = -1;
    private int activeTaskKillCount;
    private bool hasSeenFriendlyUnit;
    private bool subscribedToEvents;
    private EscortPlatformActivity activeEscortPlatform;

    public event Action<GameSessionState> OnStateChanged;
    public event Action<GameSessionTaskDefinition> OnTaskStarted;
    public event Action<GameSessionTaskDefinition> OnTaskCompleted;
    public event Action<float> OnThreatChanged;

    public GameSessionState State => state;
    public GameSessionTaskDefinition ActiveTask => activeTask;
    public float SessionTimer => sessionTimer;
    public float TaskTimer => taskTimer;
    public float ThreatLevel => threatLevel;
    public float TaskProgress01 => GetTaskProgress01();
    public int ActiveTaskCurrentAmount => GetTaskCurrentAmount();
    public int ActiveTaskRequiredAmount => GetTaskRequiredAmount();
    public float ActiveTaskTimeRemaining => activeTask == null ? 0f : Mathf.Max(0f, activeTask.Duration - taskTimer);
    public string ActiveTaskProgressText => GetTaskProgressText();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        ResetRuntime();
    }

    private void Start()
    {
        TrySubscribeToEvents();

        if (config != null && config.AutoStart)
            StartSession(config.StartDelay);
    }

    private void OnEnable()
    {
        TrySubscribeToEvents();
    }

    protected override void OnDestroy()
    {
        UnsubscribeFromEvents();
        base.OnDestroy();
    }

    private void Update()
    {
        TrySubscribeToEvents();

        if (!IsSessionRunning())
            return;

        float dt = Time.deltaTime;
        sessionTimer += dt;
        taskTimer += dt;

        UpdateThreat(dt);
        UpdateScheduledEvents();
        UpdateTimedEventRules();
        UpdateSquadDefeatCheck();
        UpdateActiveTask();
    }

    public void StartSession()
    {
        StartSession(0f);
    }

    public void StartSession(float delay)
    {
        if (config == null)
        {
            Debug.LogWarning($"{nameof(GameSessionManager)} cannot start without a GameSessionConfig.");
            return;
        }

        CancelInvoke(nameof(StartSessionNow));

        if (delay > 0f)
        {
            Invoke(nameof(StartSessionNow), delay);
            return;
        }

        StartSessionNow();
    }

    [ContextMenu("Start Session Now")]
    private void StartSessionNow()
    {
        if (config == null)
            return;

        ResetRuntime();
        SetThreat(config.StartingThreat);

        SetState(GameSessionState.Briefing);
        Log($"Session started: {config.SessionName}");

        TriggerRules(GameSessionEventTiming.SessionStarted, null);

        if (config.Tasks == null || config.Tasks.Length == 0)
        {
            SetState(GameSessionState.Playing);
            return;
        }

        StartTask(0);
    }

    [ContextMenu("Complete Current Task")]
    public void CompleteCurrentTask()
    {
        if (!IsSessionRunning() || activeTask == null)
            return;

        CompleteActiveTask();
    }

    [ContextMenu("Force Next Task")]
    public void ForceNextTask()
    {
        if (config == null)
            return;

        int nextIndex = Mathf.Clamp(activeTaskIndex + 1, 0, Mathf.Max(0, config.Tasks.Length - 1));
        StartTask(nextIndex);
    }

    [ContextMenu("Trigger First Manual Event")]
    public void TriggerFirstManualEvent()
    {
        if (config == null)
            return;

        for (int i = 0; i < eventRuntimes.Count; i++)
        {
            if (eventRuntimes[i].Rule.Timing == GameSessionEventTiming.Manual)
            {
                TryTriggerRule(eventRuntimes[i], activeTask);
                return;
            }
        }
    }

    public bool TriggerEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        for (int i = 0; i < eventRuntimes.Count; i++)
        {
            if (eventRuntimes[i].Rule.Id != eventId)
                continue;

            return TryTriggerRule(eventRuntimes[i], activeTask);
        }

        return false;
    }

    public void AddThreat(float amount)
    {
        SetThreat(threatLevel + amount);
    }

    public void SetThreat(float value)
    {
        float maxThreat = config != null ? config.MaxThreat : 100f;
        float newThreat = Mathf.Clamp(value, 0f, Mathf.Max(0f, maxThreat));
        if (Mathf.Approximately(threatLevel, newThreat))
            return;

        threatLevel = newThreat;
        OnThreatChanged?.Invoke(threatLevel);
    }

    public void WinSession()
    {
        if (state == GameSessionState.Victory)
            return;

        SetState(GameSessionState.Victory);
        activeTask = null;
        Log("Session victory.");

        SceneDirector.OpenScenesThroughLoadingScreen(SceneDirector.WINLOADINGSCREEN,
            SceneDirector.MAINMENU, SceneDirector.CITY);
    }

    public void FailSession()
    {
        if (state == GameSessionState.Defeat)
            return;

        SetState(GameSessionState.Defeat);
        activeTask = null;
        Log("Session failed.");

        SceneDirector.OpenScenesThroughLoadingScreen(SceneDirector.LOSELOADINGSCREEN,
            SceneDirector.MAINMENU, SceneDirector.CITY);
    }

    private void ResetRuntime()
    {
        sessionTimer = 0f;
        taskTimer = 0f;
        activeTaskIndex = -1;
        activeTask = null;
        activeTaskKillCount = 0;
        activeEscortPlatform = null;
        hasSeenFriendlyUnit = false;
        scheduledEvents.Clear();
        BuildEventRuntimes();
    }

    private void BuildEventRuntimes()
    {
        eventRuntimes.Clear();

        if (config == null || config.EventRules == null)
            return;

        for (int i = 0; i < config.EventRules.Length; i++)
        {
            GameSessionEventRule rule = config.EventRules[i];
            if (rule == null)
                continue;

            eventRuntimes.Add(new EventRuleRuntime
            {
                Rule = rule,
                NextTriggerTime = Mathf.Max(0f, rule.Delay),
                WasBelowThreatGate = true
            });
        }
    }

    private void StartTask(int index)
    {
        if (config == null || config.Tasks == null || index < 0 || index >= config.Tasks.Length)
        {
            WinSession();
            return;
        }

        StopActiveTaskActivity();

        activeTaskIndex = index;
        activeTask = config.Tasks[index];
        activeTaskKillCount = 0;
        taskTimer = 0f;
        BeginTaskActivity(activeTask);

        SetState(ResolveStateForTask(activeTask));
        ReportTaskStarted(activeTask);
        PlayTaskMonologue(activeTask.MonologueOnStart);
        OnTaskStarted?.Invoke(activeTask);
        TriggerRules(GameSessionEventTiming.TaskStarted, activeTask);

        Log($"Task started: {activeTask.Title}");
    }

    private void CompleteActiveTask()
    {
        GameSessionTaskDefinition completedTask = activeTask;
        if (completedTask == null)
            return;

        OnTaskCompleted?.Invoke(completedTask);
        TriggerRules(GameSessionEventTiming.TaskCompleted, completedTask);
        PlayTaskMonologue(completedTask.MonologueOnComplete);
        Log($"Task completed: {completedTask.Title}");

        StopActiveTaskActivity();

        bool shouldFinishSession = completedTask.CompleteSessionOnFinish ||
                                   completedTask.Type == GameSessionTaskType.Extract ||
                                   activeTaskIndex >= config.Tasks.Length - 1;

        if (shouldFinishSession)
        {
            WinSession();
            return;
        }

        if (completedTask.AutoAdvance)
            StartTask(activeTaskIndex + 1);
    }

    private void UpdateActiveTask()
    {
        if (activeTask == null)
            return;

        bool completed = activeTask.Type switch
        {
            GameSessionTaskType.Briefing => taskTimer >= activeTask.Duration,
            GameSessionTaskType.WaitTime => taskTimer >= activeTask.Duration,
            GameSessionTaskType.SurviveTime => taskTimer >= activeTask.Duration,
            GameSessionTaskType.ReachArea => HasEnoughFriendlyUnitsInTaskArea(activeTask),
            GameSessionTaskType.Extract => HasEnoughFriendlyUnitsInTaskArea(activeTask),
            GameSessionTaskType.KillEnemies => activeTaskKillCount >= activeTask.RequiredKillCount,
            GameSessionTaskType.ActivateGenerator => IsGeneratorActivated(activeTask),
            GameSessionTaskType.EscortPlatform => IsEscortPlatformCompleted(activeTask),
            _ => false
        };

        if (completed)
            CompleteActiveTask();
    }

    private void UpdateThreat(float dt)
    {
        if (config == null || config.ThreatGainPerMinute <= 0f)
            return;

        AddThreat(config.ThreatGainPerMinute / 60f * dt);
    }

    private void UpdateScheduledEvents()
    {
        for (int i = scheduledEvents.Count - 1; i >= 0; i--)
        {
            ScheduledEvent scheduledEvent = scheduledEvents[i];
            if (scheduledEvent.FireTime > sessionTimer)
                continue;

            scheduledEvents.RemoveAt(i);
            ExecuteRule(scheduledEvent.Runtime);
        }
    }

    private void UpdateTimedEventRules()
    {
        for (int i = 0; i < eventRuntimes.Count; i++)
        {
            EventRuleRuntime runtime = eventRuntimes[i];
            GameSessionEventRule rule = runtime.Rule;

            if (rule.Timing == GameSessionEventTiming.TimedRepeat)
            {
                if (sessionTimer < runtime.NextTriggerTime)
                    continue;

                if (TryTriggerRule(runtime, activeTask))
                {
                    runtime.NextTriggerTime = sessionTimer + Mathf.Max(0.1f, rule.RepeatInterval);
                }
                else
                {
                    runtime.NextTriggerTime = sessionTimer + 0.5f;
                }

                continue;
            }

            if (rule.Timing == GameSessionEventTiming.ThreatReached)
            {
                bool threatGateReached = PassesThreatGate(rule);
                if (!threatGateReached)
                {
                    runtime.WasBelowThreatGate = true;
                    continue;
                }

                if (!runtime.WasBelowThreatGate)
                    continue;

                runtime.WasBelowThreatGate = false;
                TryTriggerRule(runtime, activeTask);
            }
        }
    }

    private void UpdateSquadDefeatCheck()
    {
        if (config == null || !config.DefeatWhenSquadIsDead)
            return;

        int aliveFriendlyCount = CountAliveFriendlyUnits();
        if (aliveFriendlyCount > 0)
        {
            hasSeenFriendlyUnit = true;
            return;
        }

        if (hasSeenFriendlyUnit)
            FailSession();
    }

    private void TriggerRules(GameSessionEventTiming timing, GameSessionTaskDefinition task)
    {
        for (int i = 0; i < eventRuntimes.Count; i++)
        {
            EventRuleRuntime runtime = eventRuntimes[i];
            if (runtime.Rule.Timing != timing)
                continue;

            TryTriggerRule(runtime, task);
        }
    }

    private bool TryTriggerRule(EventRuleRuntime runtime, GameSessionTaskDefinition task)
    {
        if (runtime == null || runtime.Rule == null)
            return false;

        GameSessionEventRule rule = runtime.Rule;
        if (!rule.Enabled)
            return false;

        if (rule.MaxTriggers > 0 && runtime.TriggerCount >= rule.MaxTriggers)
            return false;

        if (!MatchesTask(rule, task))
            return false;

        if (!PassesThreatGate(rule))
            return false;

        runtime.TriggerCount++;

        if (rule.Delay > 0f && rule.Timing != GameSessionEventTiming.TimedRepeat)
        {
            scheduledEvents.Add(new ScheduledEvent
            {
                Runtime = runtime,
                FireTime = sessionTimer + rule.Delay
            });
            return true;
        }

        ExecuteRule(runtime);
        return true;
    }

    private void ExecuteRule(EventRuleRuntime runtime)
    {
        GameSessionEventRule rule = runtime.Rule;
        Vector3 position = ResolveRulePosition(rule);

        switch (rule.Action)
        {
            case GameSessionEventAction.ReportWorldEvent:
                ReportRuleWorldEvent(rule, position);
                break;

            case GameSessionEventAction.SpawnPrefab:
                SpawnRulePrefab(rule, position);
                if (rule.ReportWorldEvent)
                    ReportRuleWorldEvent(rule, position);
                break;

            case GameSessionEventAction.ArtilleryStrike:
                RequestArtilleryStrike(position);
                if (rule.ReportWorldEvent)
                    ReportRuleWorldEvent(rule, position);
                break;

            case GameSessionEventAction.ArtilleryBarrage:
                RequestArtilleryBarrage(position);
                if (rule.ReportWorldEvent)
                    ReportRuleWorldEvent(rule, position);
                break;

            case GameSessionEventAction.AddThreat:
                AddThreat(rule.ThreatAmount);
                break;

            case GameSessionEventAction.SetThreat:
                SetThreat(rule.ThreatAmount);
                break;

            case GameSessionEventAction.CompleteCurrentTask:
                CompleteCurrentTask();
                break;
        }

        Log($"Event triggered: {rule.Id} [{rule.Action}]");
    }

    private void SpawnRulePrefab(GameSessionEventRule rule, Vector3 position)
    {
        if (rule.Prefab == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        Transform parent = rule.ParentSpawnedPrefabToManager ? transform : null;
        Instantiate(rule.Prefab, position, rotation, parent);
    }

    private void RequestArtilleryStrike(Vector3 position)
    {
        ArtilleryStrikeDirector artillery = FindFirstObjectByType<ArtilleryStrikeDirector>();
        if (artillery == null)
        {
            Debug.LogWarning("GameSessionManager tried to request artillery strike, but no ArtilleryStrikeDirector was found.");
            return;
        }

        artillery.RequestStrike(position);
    }

    private void RequestArtilleryBarrage(Vector3 position)
    {
        ArtilleryStrikeDirector artillery = FindFirstObjectByType<ArtilleryStrikeDirector>();
        if (artillery == null)
        {
            Debug.LogWarning("GameSessionManager tried to request artillery barrage, but no ArtilleryStrikeDirector was found.");
            return;
        }

        artillery.RequestBarrage(position);
    }

    private void ReportTaskStarted(GameSessionTaskDefinition task)
    {
        if (task == null || !task.ReportOnStart)
            return;

        Vector3 position = ResolveTaskPosition(task);
        string label = string.IsNullOrWhiteSpace(task.Title) ? "OBJECTIVE" : task.Title;
        WorldEventUtility.Report(
            WorldEventType.ObjectiveUpdated,
            position,
            task.Importance,
            task.Knowledge,
            Mathf.Max(1f, task.Radius),
            task.ReportDuration,
            label);
    }

    private void ReportRuleWorldEvent(GameSessionEventRule rule, Vector3 position)
    {
        WorldEventUtility.Report(
            rule.WorldEventType,
            position,
            rule.Importance,
            rule.Knowledge,
            rule.EventRadius,
            rule.EventDuration,
            rule.Label);
    }

    private static void PlayTaskMonologue(MonologueSequence sequence)
    {
        if (sequence == null)
            return;

        MonologueManager manager = MonologueManager.Instance;
        if (manager == null)
            manager = FindFirstObjectByType<MonologueManager>();

        if (manager == null)
        {
            Debug.LogWarning($"{nameof(GameSessionManager)} tried to play a monologue, but no {nameof(MonologueManager)} was found.");
            return;
        }

        manager.PlaySequence(sequence);
    }

    private Vector3 ResolveTaskPosition(GameSessionTaskDefinition task)
    {
        if (task != null &&
            task.Type == GameSessionTaskType.EscortPlatform &&
            TryGetEscortPlatform(task, out EscortPlatformActivity platform))
        {
            return platform.CurrentPosition;
        }

        if (task != null && TryGetAnchorPosition(task.TargetAnchorId, out Vector3 anchorPosition))
            return anchorPosition;

        return transform.position;
    }

    private Vector3 ResolveRulePosition(GameSessionEventRule rule)
    {
        return rule.PositionMode switch
        {
            GameSessionPositionMode.Anchor => TryGetAnchorPosition(rule.AnchorId, out Vector3 anchorPosition)
                ? anchorPosition
                : transform.position,
            GameSessionPositionMode.RandomAroundAnchor => GetRandomPointAroundAnchor(rule),
            GameSessionPositionMode.RandomInSessionArea => GetRandomPointInSessionArea(),
            GameSessionPositionMode.FriendlySquadCenter => TryGetFriendlySquadCenter(out Vector3 squadCenter)
                ? squadCenter
                : transform.position,
            _ => transform.position
        };
    }

    private Vector3 GetRandomPointAroundAnchor(GameSessionEventRule rule)
    {
        Vector3 origin = TryGetAnchorPosition(rule.AnchorId, out Vector3 anchorPosition)
            ? anchorPosition
            : transform.position;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, rule.RandomRadius);
        return origin + new Vector3(offset.x, 0f, offset.y);
    }

    private Vector3 GetRandomPointInSessionArea()
    {
        float halfX = Mathf.Max(0f, sessionAreaSize.x) * 0.5f;
        float halfZ = Mathf.Max(0f, sessionAreaSize.y) * 0.5f;

        return new Vector3(
            sessionAreaCenter.x + UnityEngine.Random.Range(-halfX, halfX),
            sessionAreaCenter.y,
            sessionAreaCenter.z + UnityEngine.Random.Range(-halfZ, halfZ));
    }

    private bool TryGetAnchorPosition(string anchorId, out Vector3 position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(anchorId))
            return false;

        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i].Transform == null || anchors[i].Id != anchorId)
                continue;

            position = anchors[i].Transform.position;
            return true;
        }

        return false;
    }

    private bool HasEnoughFriendlyUnitsInTaskArea(GameSessionTaskDefinition task)
    {
        Vector3 center = ResolveTaskPosition(task);
        int count = CountFriendlyUnitsInRadius(center, task.Radius);
        return count >= Mathf.Max(1, task.RequiredFriendlyUnits);
    }

    private bool IsGeneratorActivated(GameSessionTaskDefinition task)
    {
        Vector3 center = ResolveTaskPosition(task);
        float radiusSq = Mathf.Max(0.1f, task.Radius) * Mathf.Max(0.1f, task.Radius);
        GeneratorActivity[] generators = FindObjectsByType<GeneratorActivity>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i] == null || !generators[i].IsActivated)
                continue;

            if ((generators[i].transform.position - center).sqrMagnitude <= radiusSq)
                return true;
        }

        return false;
    }

    private void BeginTaskActivity(GameSessionTaskDefinition task)
    {
        if (task == null)
            return;

        if (task.Type != GameSessionTaskType.EscortPlatform)
            return;

        if (!TryGetEscortPlatform(task, out EscortPlatformActivity platform))
        {
            Debug.LogWarning($"{nameof(GameSessionManager)} could not find an {nameof(EscortPlatformActivity)} for task '{task.Id}'.");
            return;
        }

        activeEscortPlatform = platform;
        activeEscortPlatform.BeginEscort();
    }

    private void StopActiveTaskActivity()
    {
        if (activeEscortPlatform == null)
            return;

        if (!activeEscortPlatform.IsCompleted)
            activeEscortPlatform.PauseEscort();

        activeEscortPlatform = null;
    }

    private bool IsEscortPlatformCompleted(GameSessionTaskDefinition task)
    {
        return TryGetEscortPlatform(task, out EscortPlatformActivity platform) && platform.IsCompleted;
    }

    private float GetEscortPlatformProgress(GameSessionTaskDefinition task)
    {
        return TryGetEscortPlatform(task, out EscortPlatformActivity platform)
            ? Mathf.Clamp01(platform.Progress01)
            : 0f;
    }

    private int GetEscortPlatformProgressPercent(GameSessionTaskDefinition task)
    {
        return Mathf.RoundToInt(GetEscortPlatformProgress(task) * 100f);
    }

    private bool TryGetEscortPlatform(GameSessionTaskDefinition task, out EscortPlatformActivity platform)
    {
        platform = null;
        if (task == null)
            return false;

        string activityId = GetTaskActivityId(task);
        if (activeEscortPlatform != null && MatchesEscortPlatform(activeEscortPlatform, activityId))
        {
            platform = activeEscortPlatform;
            return true;
        }

        EscortPlatformActivity[] platforms = FindObjectsByType<EscortPlatformActivity>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < platforms.Length; i++)
        {
            if (!MatchesEscortPlatform(platforms[i], activityId))
                continue;

            platform = platforms[i];
            return true;
        }

        return false;
    }

    private static bool MatchesEscortPlatform(EscortPlatformActivity platform, string activityId)
    {
        if (platform == null)
            return false;

        if (string.IsNullOrWhiteSpace(activityId))
            return true;

        return string.Equals(platform.ActivityId, activityId, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTaskActivityId(GameSessionTaskDefinition task)
    {
        if (task == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(task.ActivityId))
            return task.ActivityId;

        return task.TargetAnchorId;
    }

    private int CountFriendlyUnitsInRadius(Vector3 center, float radius)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return 0;

        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<LocalTransform>());

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        int count = 0;
        float radiusSq = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                    continue;

                Unit unit = em.GetComponentData<Unit>(entity);
                if (unit.faction != Faction.Friendly)
                    continue;

                LocalTransform localTransform = em.GetComponentData<LocalTransform>(entity);
                float3 delta = localTransform.Position - new float3(center.x, center.y, center.z);
                delta.y = 0f;

                if (math.lengthsq(delta) <= radiusSq)
                    count++;
            }
        }
        finally
        {
            entities.Dispose();
            query.Dispose();
        }

        return count;
    }

    private int CountAliveFriendlyUnits()
    {
        if (!TryGetEntityManager(out EntityManager em))
            return 0;

        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Unit>());
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        int count = 0;

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                    continue;

                if (em.GetComponentData<Unit>(entity).faction == Faction.Friendly)
                    count++;
            }
        }
        finally
        {
            entities.Dispose();
            query.Dispose();
        }

        return count;
    }

    private bool TryGetFriendlySquadCenter(out Vector3 center)
    {
        center = default;
        if (!TryGetEntityManager(out EntityManager em))
            return false;

        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<LocalTransform>());

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        float3 sum = float3.zero;
        int count = 0;

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || em.HasComponent<DeadUnit>(entity))
                    continue;

                Unit unit = em.GetComponentData<Unit>(entity);
                if (unit.faction != Faction.Friendly)
                    continue;

                sum += em.GetComponentData<LocalTransform>(entity).Position;
                count++;
            }
        }
        finally
        {
            entities.Dispose();
            query.Dispose();
        }

        if (count == 0)
            return false;

        float3 average = sum / count;
        center = new Vector3(average.x, average.y, average.z);
        return true;
    }

    private void OnUnitDeath(UnitDeathEvent evt)
    {
        if (!IsSessionRunning() || activeTask == null)
            return;

        if (activeTask.Type != GameSessionTaskType.KillEnemies)
            return;

        if (evt.Faction == activeTask.KillTargetFaction)
            activeTaskKillCount++;
    }

    private bool TryGetEntityManager(out EntityManager em)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            em = default;
            return false;
        }

        em = world.EntityManager;
        return true;
    }

    private bool MatchesTask(GameSessionEventRule rule, GameSessionTaskDefinition task)
    {
        if (string.IsNullOrWhiteSpace(rule.TaskId))
            return true;

        return task != null && task.Id == rule.TaskId;
    }

    private bool PassesThreatGate(GameSessionEventRule rule)
    {
        if (rule.MinThreat > 0f && threatLevel < rule.MinThreat)
            return false;

        if (rule.MaxThreat > 0f && threatLevel > rule.MaxThreat)
            return false;

        return true;
    }

    private float GetTaskProgress01()
    {
        if (activeTask == null)
            return 0f;

        return activeTask.Type switch
        {
            GameSessionTaskType.Briefing => GetTimerProgress(activeTask.Duration),
            GameSessionTaskType.WaitTime => GetTimerProgress(activeTask.Duration),
            GameSessionTaskType.SurviveTime => GetTimerProgress(activeTask.Duration),
            GameSessionTaskType.KillEnemies => Mathf.Clamp01(activeTaskKillCount / (float)Mathf.Max(1, activeTask.RequiredKillCount)),
            GameSessionTaskType.ReachArea => HasEnoughFriendlyUnitsInTaskArea(activeTask) ? 1f : 0f,
            GameSessionTaskType.Extract => HasEnoughFriendlyUnitsInTaskArea(activeTask) ? 1f : 0f,
            GameSessionTaskType.ActivateGenerator => IsGeneratorActivated(activeTask) ? 1f : 0f,
            GameSessionTaskType.EscortPlatform => GetEscortPlatformProgress(activeTask),
            _ => 0f
        };
    }

    private int GetTaskCurrentAmount()
    {
        if (activeTask == null)
            return 0;

        return activeTask.Type switch
        {
            GameSessionTaskType.KillEnemies => activeTaskKillCount,
            GameSessionTaskType.ReachArea => CountFriendlyUnitsInRadius(ResolveTaskPosition(activeTask), activeTask.Radius),
            GameSessionTaskType.Extract => CountFriendlyUnitsInRadius(ResolveTaskPosition(activeTask), activeTask.Radius),
            GameSessionTaskType.EscortPlatform => GetEscortPlatformProgressPercent(activeTask),
            _ => Mathf.RoundToInt(TaskProgress01 * GetTaskRequiredAmount())
        };
    }

    private int GetTaskRequiredAmount()
    {
        if (activeTask == null)
            return 0;

        return activeTask.Type switch
        {
            GameSessionTaskType.KillEnemies => Mathf.Max(1, activeTask.RequiredKillCount),
            GameSessionTaskType.ReachArea => Mathf.Max(1, activeTask.RequiredFriendlyUnits),
            GameSessionTaskType.Extract => Mathf.Max(1, activeTask.RequiredFriendlyUnits),
            GameSessionTaskType.Briefing => Mathf.CeilToInt(Mathf.Max(0f, activeTask.Duration)),
            GameSessionTaskType.WaitTime => Mathf.CeilToInt(Mathf.Max(0f, activeTask.Duration)),
            GameSessionTaskType.SurviveTime => Mathf.CeilToInt(Mathf.Max(0f, activeTask.Duration)),
            GameSessionTaskType.EscortPlatform => 100,
            _ => 1
        };
    }

    private string GetTaskProgressText()
    {
        if (activeTask == null)
            return string.Empty;

        return activeTask.Type switch
        {
            GameSessionTaskType.KillEnemies => $"{activeTaskKillCount} / {Mathf.Max(1, activeTask.RequiredKillCount)}",
            GameSessionTaskType.ReachArea => $"{CountFriendlyUnitsInRadius(ResolveTaskPosition(activeTask), activeTask.Radius)} / {Mathf.Max(1, activeTask.RequiredFriendlyUnits)}",
            GameSessionTaskType.Extract => $"{CountFriendlyUnitsInRadius(ResolveTaskPosition(activeTask), activeTask.Radius)} / {Mathf.Max(1, activeTask.RequiredFriendlyUnits)}",
            GameSessionTaskType.ActivateGenerator => IsGeneratorActivated(activeTask) ? "ONLINE" : "OFFLINE",
            GameSessionTaskType.EscortPlatform => $"{GetEscortPlatformProgressPercent(activeTask)}%",
            GameSessionTaskType.Briefing => $"{Mathf.CeilToInt(ActiveTaskTimeRemaining)}s",
            GameSessionTaskType.WaitTime => $"{Mathf.CeilToInt(ActiveTaskTimeRemaining)}s",
            GameSessionTaskType.SurviveTime => $"{Mathf.CeilToInt(ActiveTaskTimeRemaining)}s",
            GameSessionTaskType.Manual => "IN PROGRESS",
            _ => string.Empty
        };
    }

    private float GetTimerProgress(float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(taskTimer / duration);
    }

    private GameSessionState ResolveStateForTask(GameSessionTaskDefinition task)
    {
        return task.Type switch
        {
            GameSessionTaskType.Briefing => GameSessionState.Briefing,
            GameSessionTaskType.Extract => GameSessionState.Extraction,
            _ => GameSessionState.Playing
        };
    }

    private bool IsSessionRunning()
    {
        return state == GameSessionState.Briefing ||
               state == GameSessionState.Playing ||
               state == GameSessionState.Extraction;
    }

    private void SetState(GameSessionState nextState)
    {
        if (state == nextState)
            return;

        state = nextState;
        OnStateChanged?.Invoke(state);
    }

    private void TrySubscribeToEvents()
    {
        if (subscribedToEvents)
            return;

        EventMediator mediator = FindFirstObjectByType<EventMediator>();
        if (mediator == null)
            return;

        mediator.OnUnitDeath += OnUnitDeath;
        subscribedToEvents = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!subscribedToEvents)
            return;

        EventMediator mediator = FindFirstObjectByType<EventMediator>();
        if (mediator != null)
            mediator.OnUnitDeath -= OnUnitDeath;

        subscribedToEvents = false;
    }

    private void Log(string message)
    {
        if (logDebug)
            Debug.Log($"[{nameof(GameSessionManager)}] {message}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.12f);
        Gizmos.DrawCube(sessionAreaCenter, new Vector3(sessionAreaSize.x, 0.1f, sessionAreaSize.y));

        if (anchors == null)
            return;

        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i].Transform == null)
                continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchors[i].Transform.position, 1f);
        }
    }
}
