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

    [Header("Events")]
    [SerializeField] private bool pingUnitDeaths = true;
    [SerializeField] private float defaultPingDuration = 4f;

    [Header("Camera")]
    [SerializeField] private bool clickToMoveCamera = true;
    [SerializeField] private bool autoFindCameraTarget = true;
    [SerializeField] private Transform cameraTarget;

    private readonly List<VisualElement> unitMarkerPool = new();
    private readonly List<MinimapPing> pings = new();

    private VisualElement hud;
    private VisualElement minimap;
    private VisualElement markerLayer;
    private VisualElement pingLayer;
    private Label signalLabel;

    private World cachedWorld;
    private EntityQuery unitQuery;
    private bool hasUnitQuery;
    private bool subscribedToEvents;
    private float refreshTimer;

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

        if (clickToMoveCamera)
        {
            frame.RegisterCallback<PointerDownEvent>(OnMinimapPointerDown);
        }
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

    private void PlaceMarker(VisualElement marker, float3 worldPosition, float size)
    {
        PlaceElement(marker, new Vector3(worldPosition.x, worldPosition.y, worldPosition.z), size);
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

        float mapSize = GetResolvedMapSize();
        return new Vector2(u * mapSize, (1f - v) * mapSize);
    }

    private Vector3 MinimapPositionToWorld(Vector2 localPosition)
    {
        FogOfWarSettings settings = GetMapSettings();
        float mapSize = GetResolvedMapSize();
        float u = Mathf.Clamp01(localPosition.x / mapSize);
        float v = 1f - Mathf.Clamp01(localPosition.y / mapSize);

        Vector2 half = new(settings.WorldSize.x * 0.5f, settings.WorldSize.y * 0.5f);
        Vector2 min = new(settings.WorldCenter.x - half.x, settings.WorldCenter.y - half.y);

        return new Vector3(
            min.x + u * settings.WorldSize.x,
            0f,
            min.y + v * settings.WorldSize.y);
    }

    private float GetResolvedMapSize()
    {
        if (minimap == null)
            return minimapSize;

        float width = minimap.resolvedStyle.width;
        float height = minimap.resolvedStyle.height;

        if (width <= 1f || height <= 1f)
            return minimapSize - 52f;

        return Mathf.Min(width, height);
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
        subscribedToEvents = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!subscribedToEvents || EventMediator.Instance == null)
            return;

        EventMediator.Instance.OnUnitDeath -= OnUnitDeath;
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
