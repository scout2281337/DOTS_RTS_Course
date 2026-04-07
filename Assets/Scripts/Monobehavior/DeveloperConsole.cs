using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class DeveloperCheats
{
    public static bool Enabled { get; set; }
}

public static class DeveloperDebugEvents
{
    public static bool Enabled { get; set; }
}

public class DeveloperConsole : MonoBehaviour
{
    private enum TargetMode
    {
        Selected,
        AllFriendly,
        UnitClass
    }

    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private bool openOnStart;

    private bool isOpen;
    private bool requestFocus;
    private string commandInput = string.Empty;
    private Vector2 scrollPosition;
    private readonly List<string> outputLines = new();

    private TargetMode targetMode = TargetMode.Selected;
    private UnitClass targetUnitClass = UnitClass.Raider;

    private static readonly Dictionary<string, UnitClass> UnitAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["raider"] = UnitClass.Raider,
        ["arsonist"] = UnitClass.Arsonist,
        ["juggernaut"] = UnitClass.Juggernaut,
        ["sniper"] = UnitClass.Sniper
    };

    private static readonly Dictionary<string, ModuleEffectType> ModuleAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["berserk"] = ModuleEffectType.Berserker,
        ["ricochet"] = ModuleEffectType.Ricochet,
        ["acid"] = ModuleEffectType.AcidBullets,
        ["acidbullets"] = ModuleEffectType.AcidBullets,
        ["energyvampire"] = ModuleEffectType.EnergyVampire,
        ["battery"] = ModuleEffectType.ExtraBattery,
        ["extrabattery"] = ModuleEffectType.ExtraBattery,
        ["echo"] = ModuleEffectType.DeafeningEcho,
        ["deafeningecho"] = ModuleEffectType.DeafeningEcho,
        ["vampirism"] = ModuleEffectType.Vampirism,
        ["bloody"] = ModuleEffectType.BloodySpeedUp,
        ["bloodyspeed"] = ModuleEffectType.BloodySpeedUp,
        ["supply"] = ModuleEffectType.SupplyLines,
        ["supplylines"] = ModuleEffectType.SupplyLines,
        ["doubleshell"] = ModuleEffectType.DoubleShell
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<DeveloperConsole>() != null)
            return;

        var go = new GameObject("DeveloperConsole");
        DontDestroyOnLoad(go);
        go.AddComponent<DeveloperConsole>();
    }

    private void Awake()
    {
        isOpen = openOnStart;
        requestFocus = isOpen;
        Print("Dev console ready. Type `help`.");
    }

    private void Update()
    {
        PollDeathEvents();

        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            requestFocus = isOpen;
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
            return;

        const float width = 760f;
        const float height = 420f;
        Rect rect = new Rect(20f, 20f, width, height);
        GUILayout.BeginArea(rect, GUI.skin.box);

        GUILayout.Label($"Developer Console | cheats: {(DeveloperCheats.Enabled ? "ON" : "OFF")} | events: {(DeveloperDebugEvents.Enabled ? "ON" : "OFF")} | target: {DescribeTarget()}");

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(height - 90f));
        foreach (string line in outputLines)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView();

        GUI.SetNextControlName("DevConsoleInput");
        commandInput = GUILayout.TextField(commandInput);

        if (requestFocus)
        {
            GUI.FocusControl("DevConsoleInput");
            requestFocus = false;
        }

        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                ExecuteCommand(commandInput);
                commandInput = string.Empty;
                requestFocus = true;
                e.Use();
            }
        }

        GUILayout.EndArea();
    }

    private void ExecuteCommand(string rawInput)
    {
        string input = (rawInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(input))
            return;

        Print($"> {input}");

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "help":
                PrintHelp();
                return;
            case "clear":
                outputLines.Clear();
                return;
            case "status":
                Print($"Cheats: {(DeveloperCheats.Enabled ? "ON" : "OFF")}, events: {(DeveloperDebugEvents.Enabled ? "ON" : "OFF")}, target: {DescribeTarget()}");
                return;
            case "cheats":
                HandleCheatsCommand(parts);
                return;
            case "events":
                HandleEventsCommand(parts);
                return;
            case "target":
                HandleTargetCommand(parts);
                return;
            case "units":
                PrintUnits();
                return;
        }

        if (!DeveloperCheats.Enabled)
        {
            Print("Cheats are OFF. Use `cheats on` first.");
            return;
        }

        switch (command)
        {
            case "hp":
                HandleHealthCommand(parts);
                return;
            case "module":
                HandleModuleCommand(parts);
                return;
            default:
                Print("Unknown command. Type `help`.");
                return;
        }
    }

    private void HandleCheatsCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Print($"Cheats are {(DeveloperCheats.Enabled ? "ON" : "OFF")}.");
            return;
        }

        string mode = parts[1].ToLowerInvariant();
        switch (mode)
        {
            case "on":
                DeveloperCheats.Enabled = true;
                Print("Cheats enabled.");
                break;
            case "off":
                DeveloperCheats.Enabled = false;
                Print("Cheats disabled.");
                break;
            case "toggle":
                DeveloperCheats.Enabled = !DeveloperCheats.Enabled;
                Print($"Cheats {(DeveloperCheats.Enabled ? "enabled" : "disabled")}.");
                break;
            default:
                Print("Usage: cheats on|off|toggle");
                break;
        }
    }

    private void HandleTargetCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Print($"Current target: {DescribeTarget()}");
            return;
        }

        string arg = parts[1].ToLowerInvariant();
        if (arg == "selected")
        {
            targetMode = TargetMode.Selected;
            Print("Target mode set to selected units.");
            return;
        }

        if (arg == "all")
        {
            targetMode = TargetMode.AllFriendly;
            Print("Target mode set to all friendly units.");
            return;
        }

        if (UnitAliases.TryGetValue(arg, out var unitClass))
        {
            targetMode = TargetMode.UnitClass;
            targetUnitClass = unitClass;
            Print($"Target mode set to {unitClass}.");
            return;
        }

        Print("Usage: target selected|all|raider|juggernaut|arsonist|sniper");
    }

    private void HandleEventsCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Print($"Events are {(DeveloperDebugEvents.Enabled ? "ON" : "OFF")}.");
            return;
        }

        string mode = parts[1].ToLowerInvariant();
        switch (mode)
        {
            case "on":
                DeveloperDebugEvents.Enabled = true;
                Print("Event debug enabled.");
                break;
            case "off":
                DeveloperDebugEvents.Enabled = false;
                Print("Event debug disabled.");
                break;
            case "toggle":
                DeveloperDebugEvents.Enabled = !DeveloperDebugEvents.Enabled;
                Print($"Event debug {(DeveloperDebugEvents.Enabled ? "enabled" : "disabled")}.");
                break;
            default:
                Print("Usage: events on|off|toggle");
                break;
        }
    }

    private void HandleHealthCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            Print("Usage: hp add <value> | hp set <value>");
            return;
        }

        string mode = parts[1].ToLowerInvariant();
        string valueToken = parts[2].Replace(',', '.');
        if (!float.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            Print("Invalid number.");
            return;
        }

        if (!TryGetEntityManager(out var em))
        {
            Print("EntityManager is not ready.");
            return;
        }

        var targets = ResolveTargetUnitClasses(em);
        if (targets.Count == 0)
        {
            Print("No target units found.");
            return;
        }

        int changed = 0;
        foreach (UnitClass unitClass in targets)
        {
            if (!TryResolveFriendlyUnitEntity(unitClass, em, out var entity))
                continue;
            if (!em.HasComponent<Health>(entity))
                continue;

            var health = em.GetComponentData<Health>(entity);
            if (mode == "add")
            {
                health.healthAmount += value;
            }
            else if (mode == "set")
            {
                health.healthAmount = value;
            }
            else
            {
                Print("Usage: hp add <value> | hp set <value>");
                return;
            }

            health.healthAmount = Mathf.Clamp(health.healthAmount, 0f, health.healthAmountMax);
            health.OnHealthChanged = true;
            em.SetComponentData(entity, health);
            changed++;
        }

        Print($"Health updated for {changed} unit(s).");
    }

    private void HandleModuleCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Print("Usage: module <effectName>. Example: module ricochet");
            return;
        }

        if (!TryParseModuleEffect(parts[1], out ModuleEffectType effectType))
        {
            Print("Unknown module. Use: berserk, ricochet, acid, energyvampire, battery, echo, vampirism, bloody, supply, doubleshell");
            return;
        }

        if (!TryGetEntityManager(out var em))
        {
            Print("EntityManager is not ready.");
            return;
        }

        var targets = ResolveTargetUnitClasses(em);
        if (targets.Count == 0)
        {
            Print("No target units found.");
            return;
        }

        if (ModuleManager.Instance == null)
        {
            Print("ModuleManager not found.");
            return;
        }

        int granted = 0;
        foreach (UnitClass unitClass in targets)
        {
            if (ModuleManager.Instance.GiveModuleToUnit(unitClass, effectType))
                granted++;
        }

        Print($"Module `{effectType}` granted to {granted} unit(s).");
    }

    private void PrintUnits()
    {
        if (!TryGetEntityManager(out var em))
        {
            Print("EntityManager is not ready.");
            return;
        }

        if (FriendlyUnitManager.Instance == null || FriendlyUnitManager.Instance.unitEntityDict.Count == 0)
        {
            Print("No friendly units registered yet.");
            return;
        }

        foreach (var kv in FriendlyUnitManager.Instance.unitEntityDict)
        {
            UnitClass unitClass = kv.Key;
            Entity entity = kv.Value;
            if (!em.Exists(entity))
            {
                Print($"- {unitClass}: entity missing");
                continue;
            }

            string hpText = "no health";
            if (em.HasComponent<Health>(entity))
            {
                var hp = em.GetComponentData<Health>(entity);
                hpText = $"{hp.healthAmount:0.##}/{hp.healthAmountMax:0.##}";
            }

            bool isSelected = em.HasComponent<Selected>(entity) && em.IsComponentEnabled<Selected>(entity);
            Print($"- {unitClass}: hp {hpText}, selected={(isSelected ? "yes" : "no")}");
        }
    }

    private HashSet<UnitClass> ResolveTargetUnitClasses(EntityManager em)
    {
        var result = new HashSet<UnitClass>();

        if (FriendlyUnitManager.Instance == null)
            return result;

        switch (targetMode)
        {
            case TargetMode.UnitClass:
                result.Add(targetUnitClass);
                break;

            case TargetMode.AllFriendly:
                foreach (var unitClass in FriendlyUnitManager.Instance.unitEntityDict.Keys)
                    result.Add(unitClass);
                break;

            case TargetMode.Selected:
                {
                    EntityQuery query = em.CreateEntityQuery(
                        ComponentType.ReadOnly<Unit>(),
                        ComponentType.ReadOnly<Selected>());
                    var entities = query.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        if (!em.Exists(entity))
                            continue;
                        if (!em.IsComponentEnabled<Selected>(entity))
                            continue;
                        Unit unit = em.GetComponentData<Unit>(entity);
                        if (FriendlyUnitManager.Instance.unitEntityDict.ContainsKey(unit.Class))
                            result.Add(unit.Class);
                    }
                    entities.Dispose();
                }
                break;
        }

        return result;
    }

    private static bool TryResolveFriendlyUnitEntity(UnitClass unitClass, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        if (FriendlyUnitManager.Instance == null)
            return false;
        if (!FriendlyUnitManager.Instance.unitEntityDict.TryGetValue(unitClass, out entity))
            return false;
        return em.Exists(entity);
    }

    private static bool TryGetEntityManager(out EntityManager em)
    {
        em = default;
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return false;
        em = world.EntityManager;
        return true;
    }

    private static bool TryParseModuleEffect(string raw, out ModuleEffectType effectType)
    {
        if (Enum.TryParse(raw, true, out effectType) && effectType != ModuleEffectType.None)
            return true;

        return ModuleAliases.TryGetValue(raw, out effectType);
    }

    private void PollDeathEvents()
    {
        if (!TryGetEntityManager(out var em))
            return;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<EventHub>(),
            ComponentType.ReadOnly<UnitDeathConsoleEvent>());

        if (query.IsEmptyIgnoreFilter)
        {
            query.Dispose();
            return;
        }

        Entity hub = query.GetSingletonEntity();
        query.Dispose();

        var events = em.GetBuffer<UnitDeathConsoleEvent>(hub);
        var ricochetEvents = em.GetBuffer<RicochetConsoleEvent>(hub);
        bool hasDeathEvents = events.Length > 0;
        bool hasRicochetEvents = ricochetEvents.Length > 0;
        if (!hasDeathEvents && !hasRicochetEvents)
            return;

        if (!DeveloperDebugEvents.Enabled)
        {
            events.Clear();
            ricochetEvents.Clear();
            return;
        }

        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            string victimText = e.HasVictimUnit
                ? $"{e.VictimClass} [{e.VictimFaction}]"
                : "Unknown victim";
            string killerText = e.HasKillerUnit
                ? $"{e.KillerClass} [{e.KillerFaction}]"
                : "Unknown killer";
            Print($"DEATH: {victimText} killed by {killerText}");
        }

        for (int i = 0; i < ricochetEvents.Length; i++)
        {
            var e = ricochetEvents[i];
            Print($"RICOCHET: {e.KillerClass} [{e.KillerFaction}] -> {e.TargetClass} [{e.TargetFaction}] dmg={e.DamageAmount:0.##}");
        }

        events.Clear();
        ricochetEvents.Clear();
    }

    private string DescribeTarget()
    {
        return targetMode switch
        {
            TargetMode.Selected => "selected",
            TargetMode.AllFriendly => "all",
            TargetMode.UnitClass => targetUnitClass.ToString(),
            _ => "unknown"
        };
    }

    private void PrintHelp()
    {
        Print("Commands:");
        Print("help");
        Print("clear");
        Print("status");
        Print("cheats on|off|toggle");
        Print("events on|off|toggle");
        Print("units");
        Print("target selected|all|raider|juggernaut|arsonist|sniper");
        Print("hp add <value>   (cheats ON)");
        Print("hp set <value>   (cheats ON)");
        Print("module <name>    (cheats ON)");
        Print("module names: berserk, ricochet, acid, energyvampire, battery, echo, vampirism, bloody, supply, doubleshell");
    }

    private void Print(string message)
    {
        outputLines.Add(message);
        if (outputLines.Count > 200)
            outputLines.RemoveAt(0);
        scrollPosition.y = float.MaxValue;
    }
}
