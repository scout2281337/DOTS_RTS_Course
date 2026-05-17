using UnityEngine;

public enum MinimapEnvironmentMarkerKind
{
    Building,
    Obstacle,
    Landmark,
    Road
}

public enum MinimapEnvironmentFootprintSource
{
    Auto,
    Collider,
    Renderer,
    Manual
}

public struct MinimapEnvironmentFootprint
{
    public Vector3 Center;
    public Vector2 Size;
    public float RotationY;
    public MinimapEnvironmentMarkerKind Kind;
}

[DisallowMultipleComponent]
public class MinimapEnvironmentMarker : MonoBehaviour
{
    [SerializeField] private MinimapEnvironmentMarkerKind kind = MinimapEnvironmentMarkerKind.Building;
    [SerializeField] private MinimapEnvironmentFootprintSource footprintSource = MinimapEnvironmentFootprintSource.Auto;
    [SerializeField] private bool visibleOnMinimap = true;
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private Vector2 manualSize = new(4f, 4f);
    [SerializeField] private Vector3 manualCenterOffset;
    [SerializeField] private float manualRotationY;
    [SerializeField] private float minimumWorldSize = 1f;

    public bool VisibleOnMinimap => visibleOnMinimap;

    public bool TryGetFootprint(out MinimapEnvironmentFootprint footprint)
    {
        footprint = default;

        if (!isActiveAndEnabled || !visibleOnMinimap)
            return false;

        bool found = footprintSource switch
        {
            MinimapEnvironmentFootprintSource.Collider => TryGetColliderFootprint(out footprint),
            MinimapEnvironmentFootprintSource.Renderer => TryGetRendererFootprint(out footprint),
            MinimapEnvironmentFootprintSource.Manual => TryGetManualFootprint(out footprint),
            _ => TryGetColliderFootprint(out footprint) ||
                 TryGetRendererFootprint(out footprint) ||
                 TryGetManualFootprint(out footprint)
        };

        if (!found)
            return false;

        footprint.Kind = kind;
        footprint.Size = new Vector2(
            Mathf.Max(minimumWorldSize, Mathf.Abs(footprint.Size.x)),
            Mathf.Max(minimumWorldSize, Mathf.Abs(footprint.Size.y)));
        return true;
    }

    private bool TryGetColliderFootprint(out MinimapEnvironmentFootprint footprint)
    {
        footprint = default;

        BoxCollider boxCollider = includeChildren
            ? GetComponentInChildren<BoxCollider>()
            : GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            Vector3 scale = boxCollider.transform.lossyScale;
            footprint.Center = boxCollider.transform.TransformPoint(boxCollider.center);
            footprint.Size = new Vector2(
                Mathf.Abs(boxCollider.size.x * scale.x),
                Mathf.Abs(boxCollider.size.z * scale.z));
            footprint.RotationY = boxCollider.transform.eulerAngles.y;
            return true;
        }

        Collider[] colliders = includeChildren
            ? GetComponentsInChildren<Collider>()
            : GetComponents<Collider>();

        return TryGetBoundsFootprint(colliders, out footprint);
    }

    private bool TryGetRendererFootprint(out MinimapEnvironmentFootprint footprint)
    {
        Renderer[] renderers = includeChildren
            ? GetComponentsInChildren<Renderer>()
            : GetComponents<Renderer>();

        return TryGetBoundsFootprint(renderers, out footprint);
    }

    private bool TryGetManualFootprint(out MinimapEnvironmentFootprint footprint)
    {
        footprint = new MinimapEnvironmentFootprint
        {
            Center = transform.TransformPoint(manualCenterOffset),
            Size = new Vector2(Mathf.Abs(manualSize.x), Mathf.Abs(manualSize.y)),
            RotationY = transform.eulerAngles.y + manualRotationY,
            Kind = kind
        };

        return footprint.Size.x > 0f && footprint.Size.y > 0f;
    }

    private static bool TryGetBoundsFootprint(Component[] components, out MinimapEnvironmentFootprint footprint)
    {
        footprint = default;

        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
                continue;

            Bounds componentBounds = components[i] switch
            {
                Collider colliderComponent => colliderComponent.bounds,
                Renderer rendererComponent => rendererComponent.bounds,
                _ => default
            };

            if (componentBounds.size == Vector3.zero)
                continue;

            if (!hasBounds)
            {
                bounds = componentBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(componentBounds);
            }
        }

        if (!hasBounds)
            return false;

        footprint.Center = bounds.center;
        footprint.Size = new Vector2(bounds.size.x, bounds.size.z);
        footprint.RotationY = 0f;
        return true;
    }
}
