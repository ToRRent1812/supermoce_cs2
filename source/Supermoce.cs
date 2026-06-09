using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using System.Reflection;
using WASDSharedAPI;

namespace Supermoce
{
    public partial class Supermoce : BasePlugin
    {
        public static Supermoce? Instance { get; private set; }
        public ConcurrentDictionary<ulong, SkillPlayerInfo> SkillPlayer { get; } = new();
        public Random Random { get; } = new();
        public CCSGameRules? GameRules { get; set; }
        private readonly ConcurrentDictionary<string, byte> _manifestResources = new(StringComparer.OrdinalIgnoreCase);
        private void InitManifest()
        {
            _manifestResources.TryAdd("models/sprays/spray_plane.vmdl", 0);
        }
        public IWasdMenuManager? MenuManager;
        private static readonly ConcurrentDictionary<(string skill, string method), Action<object[]?>> _skillMethodCache = [];
        internal static readonly ConcurrentDictionary<string, byte> SkillsWithOnTick = new();
        internal static readonly ConcurrentDictionary<(string eventName, string skillName), byte> SkillsWithEvent = new();

        public override string ModuleName => "Supermoce";
        public override string ModuleAuthor => "D3X (dRandomSkills), Juzlus (jRandomSkills), Rabbit";
        public override string ModuleDescription => "Fork forka, który dodaje graczom supermoce";
        public override string ModuleVersion => "2.1.6";

        public override void Load(bool hotReload)
        {
            Instance = this;
            InitManifest();
            PlayerOnTick.Load();
            Event.Load();
            Command.Load();
            WASDMenuAPI.WASDMenuAPI.LoadPlugin(Instance, hotReload);
            LoadAllSkills();

            int currentFreezetime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 0;
            if (currentFreezetime < 13)
                Server.ExecuteCommand($"mp_freezetime 13");
            Server.ExecuteCommand("sv_legacy_jump 1");
        }

        internal void AddToManifest(string prop)
        {
            _manifestResources.TryAdd(prop, 0);
        }

        internal void LoadManifest(ResourceManifest manifest)
        {
            foreach (var prop in _manifestResources.Keys)
                manifest.AddResource(prop);
        }

        internal void LoadAllSkills()
        {
            string[] dispatchEvents = [
                "PlayerMakeSound", "WeaponFire", "WeaponEquip", "WeaponPickup", "WeaponReload",
                "GrenadeThrown", "BombBeginplant", "BombPlanted", "BombBegindefuse", "HostageFollows",
                "DecoyStarted", "DecoyDetonate", "MolotovDetonate", "SmokegrenadeDetonate", "SmokegrenadeExpired",
                "PlayerHurt", "PlayerJump", "PlayerBlind", "OnTakeDamage", "OnEntitySpawned", "CheckTransmit"
            ];

            foreach (var skill in Enum.GetValues(typeof(Skills)))
            {
                SkillAction(skill.ToString()!, "LoadSkill");

                string skillName = skill.ToString()!;
                Type? type = Type.GetType($"Supermoce.{skillName}");
                if (type != null && typeof(ISkill).IsAssignableFrom(type))
                {
                    foreach (var methodName in dispatchEvents)
                    {
                        MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                        if (method != null && method.DeclaringType == type)
                            SkillsWithEvent.TryAdd((methodName, skillName), 0);
                    }

                    MethodInfo? onTick = type.GetMethod("OnTick", BindingFlags.Static | BindingFlags.Public);
                    if (onTick != null && onTick.DeclaringType == type)
                        SkillsWithOnTick.TryAdd(skillName, 0);
                }
            }
        }

        internal void SkillAction(string skill, string methodName, object[]? param = null)
        {
            var key = (skill, methodName);
            var action = _skillMethodCache.GetOrAdd(key, k =>
            {
                string skillName = k.Item1;
                string methodName = k.Item2;
                string className = $"Supermoce.{skillName}";
                Type? type = Type.GetType(className);

                if (type != null && typeof(ISkill).IsAssignableFrom(type))
                {
                    MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                    if (method != null)
                        return p =>
                        {
                            try { method.Invoke(null, p); }
                            catch (Exception ex) { Server.PrintToConsole($"SkillAction {skillName}.{methodName} threw: {ex}"); }
                        };
                }
                return _ => { };
            });
            action(param);
        }

        internal new void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
        {
            var definition = new CommandDefinition(name, description, handler);
            CommandDefinitions.Add(definition);
            CommandManager.RegisterCommand(definition);
        }

        internal bool IsPlayerValid(CCSPlayerController? player) =>
            player != null && player.IsValid && player.PlayerPawn?.Value?.Health > 0;

        public uint[] footstepSoundEvents = [3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891, 3847761506, 4084367249, 1342713723, 3847761506, 2026488395, 2745524735, 2684452812, 2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967, 2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267, 809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412, 1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745, 2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910, 1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653];
        public uint[] silentSoundEvents = [2551626319, 765706800, 765706800, 2860219006, 2162652424, 2551626319, 2162652424, 117596568, 117596568, 740474905, 1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349, 2684452812, 2067683805, 2067683805, 1016523349, 4160462271, 1543118744, 585390608, 3802757032, 2302139631, 2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645, 3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066, 782454593, 4074593561, 1540837791, 3257325156];
    }

    public class SkillPlayerInfo
    {
        public required ulong SteamID { get; set; }
        public required string PlayerName { get; set; }
        public Skills Skill { get; set; }
        public Skills SpecialSkill { get; set; }
        public float? SkillChance { get; set; }
        public bool IsDrawing { get; set; }
        public string? RandomPercentage { get; set; }
    }

    public class SkillInfo(Skills skill, string name, string desc, string color, byte teamnum = 0, byte objective = 0)
    {
        public Skills Skill { get; } = skill;
        public string Name { get; } = name;
        public string Description { get; } = desc;
        public string Color { get; } = color;
        public byte TeamNumber { get; } = teamnum; //0 - all, 1 - T, 2 - CT
        public byte Objective { get; } = objective; //0 - all, 1 - bomb, 2 - hostage

    }

    public static class SkillData
    {
        public static ConcurrentBag<SkillInfo> Skills { get; } = [];
        public static ConcurrentDictionary<Skills, PassiveSkillConfig> PassiveSkillConfigs { get; } = [];
        public static ConcurrentDictionary<Skills, ActiveSkillConfig> ActiveSkillConfigs { get; } = [];
    }
}
