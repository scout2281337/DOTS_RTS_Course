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

    private Vector2 selectionStartMousePosition;

    private enum SelectionMode
    {
        Replace,
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
            //Debug.Log("указание движения отработало");
        }
    }

    // =========================
    // SELECTION
    // =========================

    private void HandleSelection()
    {
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        SelectionMode mode = GetSelectionMode();

        Rect selectionRect = GetSelectionAreaRect();
        bool isMultiple = selectionRect.width + selectionRect.height > 40f;

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

        for (int i = 0; i < transforms.Length; i++)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transforms[i].Position);
            hits[i] = rect.Contains(screenPos);
        }

        ApplySelection(em, entities, hits, mode);

        entities.Dispose();
        transforms.Dispose();
        hits.Dispose();
    }

    private void HandleSingleSelection(EntityManager em, SelectionMode mode)
    {
        EntityQuery query = em.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        var physicsWorld = query.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

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

        if (physicsWorld.CastRay(input, out var hit))
        {
            if (!em.HasComponent<Unit>(hit.Entity) ||
                !em.HasComponent<Selected>(hit.Entity))
                return;

            var entities = new NativeArray<Entity>(1, Allocator.Temp);
            var hits = new NativeArray<bool>(1, Allocator.Temp);

            entities[0] = hit.Entity;
            hits[0] = true;

            ApplySelection(em, entities, hits, mode);

            entities.Dispose();
            hits.Dispose();
        }
    }

    private void ApplySelection(
        EntityManager em,
        NativeArray<Entity> entities,
        NativeArray<bool> hits,
        SelectionMode mode)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            Entity e = entities[i];
            bool currentlySelected = em.IsComponentEnabled<Selected>(e);
            bool hit = hits[i];

            bool newSelected = currentlySelected;

            switch (mode)
            {
                case SelectionMode.Replace:
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
                em.SetComponentEnabled<Selected>(e, newSelected);

                var selected = em.GetComponentData<Selected>(e);
                selected.OnSelected = newSelected;
                selected.OnDeselected = !newSelected;
                em.SetComponentData(e, selected);
            }
        }
    }

    private SelectionMode GetSelectionMode()
    {
        if (Input.GetKey(KeyCode.LeftShift)) return SelectionMode.Add;
        if (Input.GetKey(KeyCode.LeftControl)) return SelectionMode.Remove;
        if (Input.GetKey(KeyCode.LeftAlt)) return SelectionMode.Invert;
        return SelectionMode.Replace;
    }

    public Rect GetSelectionAreaRect()
    {
        Vector2 end = Input.mousePosition;

        Vector2 min = Vector2.Min(selectionStartMousePosition, end);
        Vector2 max = Vector2.Max(selectionStartMousePosition, end);

        return new Rect(min, max - min);
    }

    // =========================
    // MOVE COMMAND
    // =========================

    private void HandleMoveCommand()
    {
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Vector3 target = Utilities.GetMouseWorldPosition();

        bool anySelected = !em.CreateEntityQuery(ComponentType.ReadOnly<Selected>())
            .IsEmptyIgnoreFilter;

        // Если ничего не выбрано — выбираем всех юнитов
        if (!anySelected)
        {
            EntityQuery allUnitsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Unit>()
                .Build(em);

            var allUnits = allUnitsQuery.ToEntityArray(Allocator.Temp);

            foreach (var unit in allUnits)
            {
                if (!em.HasComponent<Selected>(unit))
                    em.AddComponent<Selected>(unit);
            }

            allUnits.Dispose();
        }

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected>()
            .WithPresent<MoveOverride>()
            .Build(em);

        var entities = query.ToEntityArray(Allocator.Temp);
        var moveOverrides = query.ToComponentDataArray<MoveOverride>(Allocator.Temp);
        var positions = GenerateMovePositions(target, entities.Length);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity e = entities[i];
            var move = moveOverrides[i];

            // Сбрасываем старый путь
            if (em.HasComponent<NavPathProgress>(e))
                em.RemoveComponent<NavPathProgress>(e);

            // Назначаем новую цель
            move.targetPosition = positions[i];
            em.SetComponentData(e, move);

            // Включаем движение
            em.SetComponentEnabled<MoveOverride>(e, true);
        }

        entities.Dispose();
        moveOverrides.Dispose();
        positions.Dispose();
    }


    private NativeArray<float3> GenerateMovePositions(float3 target, int count)
    {
        var array = new NativeArray<float3>(count, Allocator.Temp);
        if (count == 0) return array;

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
                float3 offset = math.rotate(quaternion.RotateY(angle),
                    new float3(spacing * ring, 0, 0));

                array[index++] = target + offset;
            }
            ring++;
        }

        return array;
    }
}
