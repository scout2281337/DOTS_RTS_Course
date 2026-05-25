using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

public enum MinimapPingKind
{
    Info,
    Warning,
    Danger
}

public class MinimapUI : MonoBehaviour
{
    public static MinimapUI Instance { get; private set; }

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private float minimapSize = 270f;

    [Header("Map")]
    [SerializeField] private Vector2 fallbackWorldCenter = Vector2.zero;
    [SerializeField] private Vector2 fallbackWorldSize = new(120f, 120f);
    [SerializeField] private bool useFogOfWarSettings = true;
    [SerializeField] private bool clampMarkersToMap = true;

    [Header("Markers")]
    [SerializeField] private float refreshInterval = 0.05f;
    [SerializeField] private bool showFriendlyUnits = true;
    [SerializeField] private bool showVisibleEnemies = true;
    [SerializeField] private bool rotateFriendlyMarkers = true;

    [Header("Environment")]
    [SerializeField] private bool showEnvironmentMarkers = true;
    [SerializeField] private float environmentRefreshInterval = 0.35f;
    [SerializeField] private float minimumEnvironmentMarkerSize = 2f;

    [Header("Events")]
    [SerializeField] private bool pingUnitDeaths = true;
    [SerializeField] private float defaultPingDuration = 4f;

    [Header("Camera")]
    [SerializeField] private bool clickToMoveCamera = true;
    [SerializeField] private bool autoFindCameraTarget = true;
    [SerializeField] private Transform cameraTarget;

    private readonly List<VisualElement> unitMarkerPool = new();
    private readonly List<VisualElement> environmentMarkerPool = new();
    private readonly List<MinimapPing> pings = new();

    private VisualElement hud;
    private VisualElement minimap;
    private VisualElement environmentLayer;
    private VisualElement markerLayer;
    private VisualElement pingLayer;
    private VisualElement eventFeed;
    private Label signalLabel;

    private World cachedWorld;
    private EntityQuery unitQuery;
    private bool hasUnitQuery;
    private bool subscribedToEvents;
    private float refreshTimer;
    private float environmentRefreshTimer;

    private struct MinimapPing
    {
        public Vector3 Position;
        public MinimapPingKind Kind;
        public string Label;
        public float Duration;
        public float TimeLeft;
        public VisualElement Element;
        public Label Text;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildUI();
        TrySubscribeToEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromEvents();
        hasUnitQuery = false;
    }

    private void Update()
    {
        if (minimap == null)
            BuildUI();

        TrySubscribeToEvents();

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = Mathf.Max(0.01f, refreshInterval);
            RefreshUnitMarkers();
        }

        environmentRefreshTimer -= Time.deltaTime;
        if (environmentRefreshTimer <= 0f)
        {
            environmentRefreshTimer = Mathf.Max(0.05f, environmentRefreshInterval);
            RefreshEnvironmentMarkers();
        }

        UpdatePings();
    }

    public static void Ping(Vector3 worldPosition, MinimapPingKind kind = MinimapPingKind.Warning, string label = "SIGNAL", float duration = -1f)
    {
        if (Instance == null)
            return;

        Instance.AddPing(worldPosition, kind, label, duration);
    }

    public void AddPing(Vector3 worldPosition, MinimapPingKind kind = MinimapPingKind.Warning, string label = "SIGNAL", float duration = -1f)
    {
        if (pingLayer == null)
            return;

        float actualDuration = duration > 0f ? duration : defaultPingDuration;

        VisualElement ping = new VisualElement();
        ping.AddToClassList("minimap-ping");
        ping.AddToClassList(GetPingClass(kind));

        VisualElement ring = new VisualElement();
        ring.AddToClassList("minimap-ping-ring");
        ping.Add(ring);

        Label text = new Label(label);
        text.AddToClassList("minimap-ping-label");
        ping.Add(text);

        pingLayer.Add(ping);

        pings.Add(new MinimapPing
        {
            Position = worldPosition,
            Kind = kind,
            Label = label,
            Duration = actualDuration,
            TimeLeft = actualDuration,
            Element = ping,
            Text = text
        });

        if (signalLabel != null)
            signalLabel.text = label;
    }

    private void BuildUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogWarning("MinimapUI needs a UIDocument in the scene.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (styleSheet == null)
            styleSheet = Resources.Load<StyleSheet>("UI/Minimap");

        if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            root.styleSheets.Add(styleSheet);

        hud?.RemoveFromHierarchy();

        hud = new VisualElement();
        hud.AddToClassList("minimap-hud");
        hud.pickingMode = PickingMode.Ignore;
        root.Add(hud);

        VisualElement frame = new VisualElement();
        frame.AddToClassList("minimap-frame");
        frame.style.width = minimapSize;
        frame.style.height = minimapSize;
        frame.pickingMode = PickingMode.Position;
        hud.Add(frame);

        VisualElement header = new VisualElement();
        header.AddToClassList("minimap-header");
        frame.Add(header);

        Label title = new Label("TACTICAL MAP");
        title.AddToClassList("minimap-title");
        header.Add(title);

        signalLabel = new Label("NO SIGNAL");
        signalLabel.AddToClassList("minimap-signal");
        header.Add(signalLabel);

        minimap = new VisualElement();
        minimap.AddToClassList("minimap-surface");
        frame.Add(minimap);

        VisualElement grid = new VisualElement();
        grid.AddToClassList("minimap-grid");
        minimap.Add(grid);

        environmentLayer = new VisualElement();
        environmentLayer.AddToClassList("minimap-layer");
        environmentLayer.AddToClassList("minimap-environment-layer");
        minimap.Add(environmentLayer);

        pingLayer = new VisualElement();
        pingLayer.AddToClassList("minimap-layer");
        minimap.Add(pingLayer);

        markerLayer = new VisualElement();
        markerLayer.AddToClassList("minimap-layer");
        minimap.Add(markerLayer);

        VisualElement scanline = new VisualElement();
        scanline.AddToClassList("minimap-scanline");
        minimap.Add(scanline);

        VisualElement footer = new VisualElement();
        footer.AddToClassList("minimap-footer");
        frame.Add(footer);

        footer.Add(new Label("FRIENDLY"));
        footer.Add(new Label("VISIBLE HOSTILES"));

        eventFeed = new VisualElement();
        eventFeed.AddToClassList("minimap-event-feed");
        frame.Add(eventFeed);

        if (clickToMoveCamera)
        {
            frame.RegisterCallback<PointerDownEvent>(OnMinimapPointerDown);
        }
    }

    private void RefreshEnvironmentMarkers()
    {
        if (environmentLayer == null)
            return;

        if (!showEnvironmentMarkers)
        {
            HideUnusedEnvironmentMarkers(0);
            return;
        }

        MinimapEnvironmentMarker[] markers = FindObjectsByType<MinimapEnvironmentMarker>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        int usedMarkers = 0;
        for (int i = 0; i < markers.Length; i++)
        {
            MinimapEnvironmentMarker marker = markers[i];
            if (marker == null || !marker.TryGetFootprint(out MinimapEnvironmentFootprint footprint))
                continue;

            VisualElement element = GetEnvironmentMarker(usedMarkers++);
            SetupEnvironmentMarkerClass(element, footprint.Kind);
            PlaceEnvironmentMarker(element, footprint);
        }

        HideUnusedEnvironmentMarkers(usedMarkers);
    }

    private void RefreshUnitMarkers()
    {
        if (markerLayer == null || !TryGetEntityManager(out EntityManager em))
            return;

        EnsureUnitQuery(em);

        NativeArray<Entity> entities = unitQuery.ToEntityArray(Allocator.Temp);
        int usedMarkers = 0;

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                if (em.HasComponent<DeadUnit>(entity))
                    continue;

                Unit unit = em.GetComponentData<Unit>(entity);
                bool isFriendly = unit.faction == Faction.Friendly;

                if (isFriendly && !showFriendlyUnits)
                    continue;

                if (!isFriendly)
                {
                    if (!showVisibleEnemies)
                        continue;

                    if (IsHiddenByFog(em, entity))
                        continue;
                }

                LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                VisualElement marker = GetMarker(usedMarkers++);

                SetupMarkerClass(marker, isFriendly, IsSelected(em, entity));
                PlaceMarker(marker, transform.Position, GetMarkerSize(isFriendly));

                if (isFriendly && rotateFriendlyMarkers)
                {
                    float3 forward = math.forward(transform.Rotation);
                    float angle = math.degrees(math.atan2(forward.x, forward.z));
                    marker.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
                }
                else
                {
                    marker.style.rotate = new Rotate(new Angle(0f, AngleUnit.Degree));
                }
            }
        }
        finally
        {
            entities.Dispose();
        }

        for (int i = usedMarkers; i < unitMarkerPool.Count; i++)
        {
            unitMarkerPool[i].style.display = DisplayStyle.None;
        }
    }

    private void UpdatePings()
    {
        for (int i = pings.Count - 1; i >= 0; i--)
        {
            MinimapPing ping = pings[i];
            ping.TimeLeft -= Time.deltaTime;

            if (ping.TimeLeft <= 0f)
            {
                ping.Element.RemoveFromHierarchy();
                pings.RemoveAt(i);
                continue;
            }

            float age = 1f - ping.TimeLeft / ping.Duration;
            float size = Mathf.Lerp(18f, 62f, age);
            float opacity = Mathf.Lerp(0.95f, 0f, age);

            PlaceElement(ping.Element, ping.Position, size);
            ping.Element.style.opacity = opacity;
            ping.Text.style.opacity = Mathf.Clamp01(opacity * 1.4f);

            pings[i] = ping;
        }

        if (pings.Count == 0 && signalLabel != null)
            signalLabel.text = "NO SIGNAL";
    }

    private VisualElement GetMarker(int index)
    {
        while (unitMarkerPool.Count <= index)
        {
            VisualElement marker = new VisualElement();
            marker.AddToClassList("minimap-marker");
            markerLayer.Add(marker);
            unitMarkerPool.Add(marker);
        }

        VisualElement result = unitMarkerPool[index];
        result.style.display = DisplayStyle.Flex;
        return result;
    }

    private VisualElement GetEnvironmentMarker(int index)
    {
        while (environmentMarkerPool.Count <= index)
        {
            VisualElement marker = new VisualElement();
            marker.AddToClassList("minimap-environment-marker");
            environmentLayer.Add(marker);
            environmentMarkerPool.Add(marker);
        }

        VisualElement result = environmentMarkerPool[index];
        result.style.display = DisplayStyle.Flex;
        return result;
    }

    private void HideUnusedEnvironmentMarkers(int usedMarkers)
    {
        for (int i = usedMarkers; i < environmentMarkerPool.Count; i++)
        {
            environmentMarkerPool[i].style.display = DisplayStyle.None;
        }
    }

    private static bool IsHiddenByFog(EntityManager em, Entity entity)
    {
        return em.HasComponent<FogRevealable>(entity) &&
               (!em.HasComponent<FogVisible>(entity) ||
                !em.IsComponentEnabled<FogVisible>(entity));
    }

    private static bool IsSelected(EntityManager em, Entity entity)
    {
        return em.HasComponent<Selected>(entity) && em.IsComponentEnabled<Selected>(entity);
    }

    private static float GetMarkerSize(bool isFriendly)
    {
        return isFriendly ? 12f : 9f;
    }

    private void SetupMarkerClass(VisualElement marker, bool isFriendly, bool selected)
    {
        marker.RemoveFromClassList("friendly");
        marker.RemoveFromClassList("enemy");
        marker.RemoveFromClassList("selected");

        marker.AddToClassList(isFriendly ? "friendly" : "enemy");
        marker.EnableInClassList("selected", selected);
    }

    private void SetupEnvironmentMarkerClass(VisualElement marker, MinimapEnvironmentMarkerKind kind)
    {
        marker.RemoveFromClassList("building");
        marker.RemoveFromClassList("obstacle");
        marker.RemoveFromClassList("landmark");
        marker.RemoveFromClassList("road");

        marker.AddToClassList(kind switch
        {
            MinimapEnvironmentMarkerKind.Obstacle => "obstacle",
            MinimapEnvironmentMarkerKind.Landmark => "landmark",
            MinimapEnvironmentMarkerKind.Road => "road",
            _ => "building"
        });
    }

    private void PlaceMarker(VisualElement marker, float3 worldPosition, float size)
    {
        PlaceElement(marker, new Vector3(worldPosition.x, worldPosition.y, worldPosition.z), size);
    }

    private void PlaceEnvironmentMarker(VisualElement marker, MinimapEnvironmentFootprint footprint)
    {
        Vector2 minimapPosition = WorldToMinimapPosition(footprint.Center);
        Vector2 markerSize = WorldSizeToMinimapSize(footprint.Size);

        marker.style.width = markerSize.x;
        marker.style.height = markerSize.y;
        marker.style.left = minimapPosition.x - markerSize.x * 0.5f;
        marker.style.top = minimapPosition.y - markerSize.y * 0.5f;
        marker.style.rotate = new Rotate(new Angle(-footprint.RotationY, AngleUnit.Degree));
    }

    private void PlaceElement(VisualElement element, Vector3 worldPosition, float size)
    {
        Vector2 minimapPosition = WorldToMinimapPosition(worldPosition);

        element.style.width = size;
        element.style.height = size;
        element.style.left = minimapPosition.x - size * 0.5f;
        element.style.top = minimapPosition.y - size * 0.5f;
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        FogOfWarSettings settings = GetMapSettings();
        Vector2 half = new(settings.WorldSize.x * 0.5f, settings.WorldSize.y * 0.5f);
        Vector2 min = new(settings.WorldCenter.x - half.x, settings.WorldCenter.y - half.y);

        float u = (worldPosition.x - min.x) / settings.WorldSize.x;
        float v = (worldPosition.z - min.y) / settings.WorldSize.y;

        if (clampMarkersToMap)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
        }

        Vector2 mapSize = GetResolvedMapDimensions();
        return new Vector2(u * mapSize.x, (1f - v) * mapSize.y);
    }

    private Vector2 WorldSizeToMinimapSize(Vector2 worldSize)
    {
        FogOfWarSettings settings = GetMapSettings();
        Vector2 mapSize = GetResolvedMapDimensions();

        float width = settings.WorldSize.x > 0f ? worldSize.x / settings.WorldSize.x * mapSize.x : 0f;
        float height = settings.WorldSize.y > 0f ? worldSize.y / settings.WorldSize.y * mapSize.y : 0f;

        return new Vector2(
            Mathf.Max(minimumEnvironmentMarkerSize, width),
            Mathf.Max(minimumEnvironmentMarkerSize, height));
    }

    private Vector3 MinimapPositionToWorld(Vector2 localPosition)
    {
        FogOfWarSettings settings = GetMapSettings();
        Vector2 mapSize = GetResolvedMapDimensions();
        float u = Mathf.Clamp01(mapSize.x > 0f ? localPosition.x / mapSize.x : 0f);
        float v = 1f - Mathf.Clamp01(mapSize.y > 0f ? localPosition.y / mapSize.y : 0f);

        Vector2 half = new(settings.WorldSize.x * 0.5f, settings.WorldSize.y * 0.5f);
        Vector2 min = new(settings.WorldCenter.x - half.x, settings.WorldCenter.y - half.y);

        return new Vector3(
            min.x + u * settings.WorldSize.x,
            0f,
            min.y + v * settings.WorldSize.y);
    }

    private Vector2 GetResolvedMapDimensions()
    {
        if (minimap == null)
            return new Vector2(minimapSize, minimapSize);

        float width = minimap.resolvedStyle.width;
        float height = minimap.resolvedStyle.height;

        if (width <= 1f || height <= 1f)
            return new Vector2(minimapSize - 52f, minimapSize - 52f);

        return new Vector2(width, height);
    }

    private FogOfWarSettings GetMapSettings()
    {
        if (useFogOfWarSettings && FogOfWarSettingsAuthoring.Active != null)
            return FogOfWarSettingsAuthoring.Active.ToSettings();

        return new FogOfWarSettings
        {
            WorldCenter = fallbackWorldCenter,
            WorldSize = math.max(new float2(fallbackWorldSize.x, fallbackWorldSize.y), new float2(1f, 1f))
        };
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

    private void EnsureUnitQuery(EntityManager em)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (hasUnitQuery && cachedWorld == world)
            return;

        cachedWorld = world;
        unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Unit>(),
            ComponentType.ReadOnly<LocalTransform>());
        hasUnitQuery = true;
    }

    private void OnMinimapPointerDown(PointerDownEvent evt)
    {
        if (!clickToMoveCamera)
            return;

        Vector2 localPosition = evt.localPosition;

        if (evt.target is VisualElement target && target != minimap)
            localPosition = minimap.WorldToLocal(evt.position);

        Vector3 worldPosition = MinimapPositionToWorld(localPosition);
        MoveCameraTo(worldPosition);
        AddPing(worldPosition, MinimapPingKind.Info, "CAMERA", 1.2f);
        evt.StopPropagation();
    }

    private void MoveCameraTo(Vector3 worldPosition)
    {
        Transform target = ResolveCameraTarget();
        if (target == null)
            return;

        target.position = new Vector3(worldPosition.x, target.position.y, worldPosition.z);
    }

    private Transform ResolveCameraTarget()
    {
        if (cameraTarget != null)
            return cameraTarget;

        if (!autoFindCameraTarget)
            return null;

        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraTarget = cameraController.transform;
            return cameraTarget;
        }

        if (Camera.main != null)
        {
            cameraTarget = Camera.main.transform;
            return cameraTarget;
        }

        return null;
    }

    private void TrySubscribeToEvents()
    {
        if (subscribedToEvents || EventMediator.Instance == null)
            return;

        EventMediator.Instance.OnUnitDeath += OnUnitDeath;
        EventMediator.Instance.OnWorldEvent += OnWorldEvent;
        subscribedToEvents = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!subscribedToEvents || EventMediator.Instance == null)
            return;

        EventMediator.Instance.OnUnitDeath -= OnUnitDeath;
        EventMediator.Instance.OnWorldEvent -= OnWorldEvent;
        subscribedToEvents = false;
    }

    private void OnUnitDeath(UnitDeathEvent evt)
    {
        if (!pingUnitDeaths)
            return;

        bool friendly = evt.Faction == Faction.Friendly;
        AddPing(
            new Vector3(evt.Position.x, evt.Position.y, evt.Position.z),
            friendly ? MinimapPingKind.Danger : MinimapPingKind.Info,
            friendly ? "UNIT LOST" : "HOSTILE DOWN");
    }

    private void OnWorldEvent(WorldEvent evt)
    {
        string label = GetWorldEventLabel(evt);
        AddPing(
            new Vector3(evt.Position.x, evt.Position.y, evt.Position.z),
            ToPingKind(evt.Importance),
            label,
            evt.Duration);
        AddFeedEntry(label, evt.Importance);
    }

    private void AddFeedEntry(string label, WorldEventImportance importance)
    {
        if (eventFeed == null)
            return;

        Label entry = new Label(label);
        entry.AddToClassList("minimap-feed-entry");
        entry.AddToClassList(GetImportanceClass(importance));
        eventFeed.Insert(0, entry);

        while (eventFeed.childCount > 3)
            eventFeed.RemoveAt(eventFeed.childCount - 1);
    }

    private static string GetWorldEventLabel(WorldEvent evt)
    {
        if (evt.Label.Length > 0)
            return evt.Label.ToString();

        return evt.Type switch
        {
            WorldEventType.Noise => evt.Knowledge == WorldEventKnowledge.Exact ? "NOISE" : "NOISE AREA",
            WorldEventType.ZombieHorde => "HORDE",
            WorldEventType.ResourceFound => "RESOURCE",
            WorldEventType.ObjectiveUpdated => "OBJECTIVE",
            WorldEventType.UnitUnderAttack => "UNDER ATTACK",
            WorldEventType.UnitDeath => "UNIT LOST",
            WorldEventType.BossSpawn => "MAJOR THREAT",
            WorldEventType.ExtractionPoint => "EXTRACTION",
            WorldEventType.ArtilleryStrike => "ARTILLERY",
            WorldEventType.EscortPlatform => "ESCORT",
            _ => "SIGNAL"
        };
    }

    private static MinimapPingKind ToPingKind(WorldEventImportance importance)
    {
        return importance switch
        {
            WorldEventImportance.Critical => MinimapPingKind.Danger,
            WorldEventImportance.High => MinimapPingKind.Danger,
            WorldEventImportance.Medium => MinimapPingKind.Warning,
            _ => MinimapPingKind.Info
        };
    }

    private static string GetImportanceClass(WorldEventImportance importance)
    {
        return importance switch
        {
            WorldEventImportance.Critical => "critical",
            WorldEventImportance.High => "high",
            WorldEventImportance.Medium => "medium",
            _ => "low"
        };
    }

    private static string GetPingClass(MinimapPingKind kind)
    {
        return kind switch
        {
            MinimapPingKind.Danger => "danger",
            MinimapPingKind.Warning => "warning",
            _ => "info"
        };
    }
}
