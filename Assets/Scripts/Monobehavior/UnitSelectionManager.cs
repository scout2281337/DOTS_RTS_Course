using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class UnitSelectionManager : Singleton<UnitSelectionManager>
{
    public event EventHandler OnSelectionAreaStart;
    public event EventHandler OnSelectionAreaEnd;
    public event Action<Vector3, int> OnMoveCommandIssued;

    private const float MultipleSelectionThreshold = 40f;

    private Vector2 selectionStartMousePosition;

    public enum SelectionMode
    {
        Standard,
        Add,
        Remove,
        Invert
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectionStartMousePosition = Input.mousePosition;
            OnSelectionAreaStart?.Invoke(this, EventArgs.Empty);
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleSelection();
            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
        }

        if (Input.GetMouseButtonDown(1))
        {
            HandleMoveCommand();
        }
    }

    private void HandleSelection()
    {
        if (World.DefaultGameObjectInjectionWorld == null || Camera.main == null)
            return;

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        SelectionMode mode = GetSelectionMode();

        Rect selectionRect = GetSelectionAreaRect();
        bool isMultiple = selectionRect.width + selectionRect.height > MultipleSelectionThreshold;

        if (isMultiple)
        {
            HandleMultipleSelection(em, selectionRect, mode);
        }
        else
        {
            HandleSingleSelection(em, mode);
        }
    }

    private void HandleMultipleSelection(EntityManager em, Rect rect, SelectionMode mode)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalTransform, Unit>()
            .WithPresent<Selected>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var hits = new NativeArray<bool>(entities.Length, Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                if (!IsFriendlySelectableUnit(em, entities[i]))
                    continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(transforms[i].Position);
                hits[i] = screenPos.z > 0f && rect.Contains(screenPos);
            }

            ApplySelection(em, entities, hits, mode);
        }
        finally
        {
            entities.Dispose();
            transforms.Dispose();
            hits.Dispose();
        }
    }

    private void HandleSingleSelection(EntityManager em, SelectionMode mode)
    {
        Entity hitEntity = TryRaycastFriendlyUnit(em, Input.mousePosition);

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .WithPresent<Selected>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        var hits = new NativeArray<bool>(entities.Length, Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                hits[i] = entities[i] == hitEntity;
            }

            if (hitEntity == Entity.Null && mode != SelectionMode.Standard)
                return;

            ApplySelection(em, entities, hits, mode);
        }
        finally
        {
            entities.Dispose();
            hits.Dispose();
        }
    }

    public void SelectUnit(UnitClass unitClass)
    {
        if (FriendlyUnitManager.Instance == null)
            return;

        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out Entity entity))
            return;

        SelectEntity(entity, GetSelectionMode());
    }

    private void SelectEntity(Entity targetEntity, SelectionMode mode)
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!IsFriendlySelectableUnit(em, targetEntity))
            return;

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .WithPresent<Selected>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        var hits = new NativeArray<bool>(entities.Length, Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                hits[i] = entities[i] == targetEntity;
            }

            ApplySelection(em, entities, hits, mode);
        }
        finally
        {
            entities.Dispose();
            hits.Dispose();
        }
    }

    public bool HasExplicitSelection()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return false;

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .WithPresent<Selected>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsFriendlySelectableUnit(em, entity))
                    continue;

                if (em.IsComponentEnabled<Selected>(entity))
                    return true;
            }

            return false;
        }
        finally
        {
            entities.Dispose();
        }
    }

    private void ApplySelection(EntityManager em, NativeArray<Entity> entities, NativeArray<bool> hits, SelectionMode mode)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsFriendlySelectableUnit(em, entity))
                continue;

            bool currentlySelected = em.IsComponentEnabled<Selected>(entity);
            bool hit = hits[i];
            bool newSelected = currentlySelected;

            switch (mode)
            {
                case SelectionMode.Standard:
                    newSelected = hit;
                    break;
                case SelectionMode.Add:
                    if (hit) newSelected = true;
                    break;
                case SelectionMode.Remove:
                    if (hit) newSelected = false;
                    break;
                case SelectionMode.Invert:
                    if (hit) newSelected = !currentlySelected;
                    break;
            }

            if (newSelected != currentlySelected)
            {
                SetSelectionState(em, entity, newSelected);
            }
        }
    }

    private static void SetSelectionState(EntityManager em, Entity entity, bool isSelected)
    {
        em.SetComponentEnabled<Selected>(entity, isSelected);

        Selected selected = em.GetComponentData<Selected>(entity);
        selected.OnSelected = isSelected;
        selected.OnDeselected = !isSelected;
        em.SetComponentData(entity, selected);
    }

    private SelectionMode GetSelectionMode()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return SelectionMode.Add;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            return SelectionMode.Remove;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            return SelectionMode.Invert;

        return SelectionMode.Standard;
    }

    public Rect GetSelectionAreaRect()
    {
        Vector2 end = Input.mousePosition;
        Vector2 min = Vector2.Min(selectionStartMousePosition, end);
        Vector2 max = Vector2.Max(selectionStartMousePosition, end);
        return new Rect(min, max - min);
    }

    private void HandleMoveCommand()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
            return;

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        float3 target = Utilities.GetMouseWorldPosition();
        bool hasExplicitSelection = HasExplicitSelection();

        if (!hasExplicitSelection)
        {
            SelectAllFriendlyUnits(em);
            hasExplicitSelection = true;
        }

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .WithPresent<MoveOverride>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        var moveOverrides = query.ToComponentDataArray<MoveOverride>(Allocator.Temp);

        int commandableCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsFriendlyUnit(em, entity))
                continue;

            bool explicitlySelected = em.HasComponent<Selected>(entity) && em.IsComponentEnabled<Selected>(entity);
            if (hasExplicitSelection && !explicitlySelected)
                continue;

            commandableCount++;
        }

        var positions = GenerateMovePositions(target, commandableCount);

        int issuedMoveCommandCount = 0;

        try
        {
            int moveIndex = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsFriendlyUnit(em, entity))
                    continue;

                bool explicitlySelected = em.HasComponent<Selected>(entity) && em.IsComponentEnabled<Selected>(entity);
                if (hasExplicitSelection && !explicitlySelected)
                    continue;

                MoveOverride move = moveOverrides[i];

                if (em.HasComponent<NavPathProgress>(entity))
                    em.RemoveComponent<NavPathProgress>(entity);

                if (em.HasComponent<Target>(entity))
                {
                    Target targetComponent = em.GetComponentData<Target>(entity);
                    targetComponent.targetEntity = Entity.Null;
                    em.SetComponentData(entity, targetComponent);
                }

                move.targetPosition = positions[moveIndex++];
                em.SetComponentData(entity, move);
                em.SetComponentEnabled<MoveOverride>(entity, true);
            }

            issuedMoveCommandCount = moveIndex;
        }
        finally
        {
            entities.Dispose();
            moveOverrides.Dispose();
            positions.Dispose();
        }

        if (issuedMoveCommandCount > 0)
            OnMoveCommandIssued?.Invoke(new Vector3(target.x, target.y, target.z), issuedMoveCommandCount);
    }

    private void SelectAllFriendlyUnits(EntityManager em)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .WithPresent<Selected>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsFriendlySelectableUnit(em, entity))
                    continue;

                if (!em.IsComponentEnabled<Selected>(entity))
                {
                    SetSelectionState(em, entity, true);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    private Entity TryRaycastFriendlyUnit(EntityManager em, Vector2 screenPosition)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        CollisionWorld physicsWorld = query.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastInput input = new RaycastInput
        {
            Start = ray.origin,
            End = ray.origin + ray.direction * 1000f,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0
            }
        };

        if (!physicsWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            return Entity.Null;

        return IsFriendlySelectableUnit(em, hit.Entity) ? hit.Entity : Entity.Null;
    }

    private static bool IsFriendlySelectableUnit(EntityManager em, Entity entity)
    {
        return em.Exists(entity)
            && em.HasComponent<Unit>(entity)
            && em.HasComponent<Selected>(entity)
            && !em.HasComponent<DeadUnit>(entity)
            && em.GetComponentData<Unit>(entity).faction == Faction.Friendly;
    }

    private static bool IsFriendlyUnit(EntityManager em, Entity entity)
    {
        return em.Exists(entity)
            && em.HasComponent<Unit>(entity)
            && !em.HasComponent<DeadUnit>(entity)
            && em.GetComponentData<Unit>(entity).faction == Faction.Friendly;
    }

    private NativeArray<float3> GenerateMovePositions(float3 target, int count)
    {
        var array = new NativeArray<float3>(count, Allocator.Temp);
        if (count == 0)
            return array;

        array[0] = target;

        float spacing = 2.2f;
        int ring = 1;
        int index = 1;

        while (index < count)
        {
            int ringCount = 6 + ring * 2;

            for (int i = 0; i < ringCount && index < count; i++)
            {
                float angle = i * math.PI2 / ringCount;
                float3 offset = math.rotate(quaternion.RotateY(angle), new float3(spacing * ring, 0, 0));
                array[index++] = target + offset;
            }
            ring++;
        }

        return array;
    }
}
