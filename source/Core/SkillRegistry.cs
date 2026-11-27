using CounterStrikeSharp.API;
using Supermoce.src.player;
using System.Collections.Concurrent;
using System.Reflection;

namespace Supermoce
{
    public enum SkillEvent
    {
        OnTick,
        OnEntitySpawned,
        OnPlayerTakeDamage,
        OnEntityTakeDamage,
        CheckTransmit,
        PlayerMakeSound,
        WeaponFire,
        WeaponEquip,
        WeaponPickup,
        WeaponReload,
        GrenadeThrown,
        BombBeginplant,
        BombPlanted,
        BombBegindefuse,
        BombAbortplant,
        BombAbortdefuse,
        BombDefused,
        BombExploded,
        BombDropped,
        BombPickup,
        HostageFollows,
        HostageRescued,
        HostageRescuedAll,
        HostageKilled,
        HostageHurt,
        HostageStopsFollowing,
        EnterBombzone,
        ExitBombzone,
        DecoyStarted,
        DecoyDetonate,
        MolotovDetonate,
        SmokegrenadeDetonate,
        SmokegrenadeExpired,
        PlayerHurt,
        PlayerJump,
        PlayerBlind,
    }

    public sealed class SkillDescriptor
    {
        public Skills Id { get; init; }
        public Type Type { get; init; } = null!;
        public bool HasOnTick { get; init; }
        public HashSet<SkillEvent> Events { get; init; } = [];
    }

    public static class SkillRegistry
    {
        private static readonly Dictionary<Skills, SkillDescriptor> _descriptors = [];
        private static readonly HashSet<(SkillEvent, Skills)> _eventSkills = [];
        private static readonly ConcurrentDictionary<(Skills, string), Action<object[]?>> _invokers = [];

        public static void Build()
        {
            _descriptors.Clear();
            _eventSkills.Clear();
            _invokers.Clear();

            var eventNames = Enum.GetNames(typeof(SkillEvent));
            foreach (var skill in Enum.GetValues<Skills>())
            {
                Type? type = Type.GetType($"Supermoce.{skill}");
                if (type == null || !typeof(src.player.ISkill).IsAssignableFrom(type))
                {
                    Server.PrintToConsole($"[Supermoce] SkillRegistry: no class for skill {skill}, skipping.");
                    continue;
                }

                var events = new HashSet<SkillEvent>();
                foreach (var name in eventNames)
                {
                    MethodInfo? method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
                    if (method != null && method.DeclaringType == type)
                    {
                        var ev = Enum.Parse<SkillEvent>(name);
                        events.Add(ev);
                        _eventSkills.Add((ev, skill));
                    }
                }

                _descriptors[skill] = new SkillDescriptor
                {
                    Id = skill,
                    Type = type,
                    HasOnTick = events.Contains(SkillEvent.OnTick),
                    Events = events,
                };
            }
        }

        public static bool HasEvent(SkillEvent ev, Skills skill) => _eventSkills.Contains((ev, skill));

        public static bool HasOnTick(Skills skill) =>
            _descriptors.TryGetValue(skill, out var desc) && desc.HasOnTick;

        public static void Dispatch(Skills skill, SkillEvent ev, object[]? args = null) =>
            DispatchByName(skill, ev.ToString(), args);

        public static void DispatchByName(Skills skill, string methodName, object[]? args = null)
        {
            _invokers.GetOrAdd((skill, methodName), k => CreateInvoker(k.Item1, k.Item2))(args);
        }

        private static Action<object[]?> CreateInvoker(Skills skill, string methodName)
        {
            if (_descriptors.TryGetValue(skill, out var desc))
            {
                MethodInfo? method = desc.Type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                    return p =>
                    {
                        try { method.Invoke(null, p); }
                        catch (Exception ex) { Server.PrintToConsole($"SkillAction {skill}.{methodName} threw: {ex}"); }
                    };
            }
            return _ => { };
        }
    }
}
