using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using System.Reflection;
using WASDSharedAPI;

namespace jRandomSkills
{
    public partial class jRandomSkills : BasePlugin
    {
        public static jRandomSkills? Instance { get; private set; }

        public ConcurrentBag<jSkill_PlayerInfo> SkillPlayer { get; set; } = [];
        public Random Random { get; } = new();
        public CCSGameRules? GameRules { get; set; }
        public IWasdMenuManager? MenuManager;

        public override string ModuleName => "Supermoce";
        public override string ModuleAuthor => "D3X (dRandomSkills), Juzlus (jRandomSkills), Rabbit";
        public override string ModuleDescription => "Fork forka który dodaje graczom supermoce";
        public override string ModuleVersion => "1.2.0";

        public override void Load(bool hotReload)
        {
            Instance = this;
            Debug.Load();
            PlayerOnTick.Load();
            Event.Load();
            Command.Load();
            WASDMenuAPI.WASDMenuAPI.LoadPlugin(Instance, hotReload);
            LoadAllSkills();
        }

        internal void LoadAllSkills()
        {
            foreach (var skill in Enum.GetValues(typeof(Skills)))
                SkillAction(skill.ToString()!, "LoadSkill");
        }

        internal void SkillAction(string skill, string methodName, object[]? param = null)
        {
            string className = $"jRandomSkills.{skill}";
            Type? type = Type.GetType(className);

            if (type != null && typeof(ISkill).IsAssignableFrom(type))
            {
                MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, param);
            }
            else
                Server.PrintToConsole($"Could not find or load {className}");
        }

        internal new void AddCommand(string name, string description, CommandInfo.CommandCallback handler)
        {
            var definition = new CommandDefinition(name, description, handler);
            CommandDefinitions.Add(definition);
            CommandManager.RegisterCommand(definition);
        }

        internal bool IsPlayerValid(CCSPlayerController? player) =>
            player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE;

        public uint[] footstepSoundEvents = [3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891, 3847761506, 4084367249, 1342713723, 3847761506, 2026488395, 2745524735, 2684452812, 2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199, 2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967, 2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970, 3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267, 809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412, 1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848, 1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745, 2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910, 1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653];
        public uint[] silentSoundEvents = [2551626319, 765706800, 765706800, 2860219006, 2162652424, 2551626319, 2162652424, 117596568, 117596568, 740474905, 1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349, 2684452812, 2067683805, 2067683805, 1016523349, 4160462271, 1543118744, 585390608, 3802757032, 2302139631, 2546391140, 144629619, 4152012084, 4113422219, 1627020521, 2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645, 3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066, 782454593, 4074593561, 1540837791, 3257325156];
    }

    public class jSkill_PlayerInfo
    {
        public required ulong SteamID { get; set; }
        public required string PlayerName { get; set; }
        public Skills Skill { get; set; }
        public Skills SpecialSkill { get; set; }
        public float? SkillChance { get; set; }
        public bool IsDrawing { get; set; }
        public string? RandomPercentage { get; set; }
    }

    public class jSkill_SkillInfo(Skills skill, string name, string desc, string color, byte teamnum = 0)
    {
        public Skills Skill { get; } = skill;
        public string Name { get; } = name;
        public string Description { get; } = desc;
        public string Color { get; } = color;
        public byte TeamNumber { get; } = teamnum;

        public static implicit operator Skills(jSkill_SkillInfo v)
        {
            throw new NotImplementedException();
        }
    }

    public static class SkillData
    {
        public static ConcurrentBag<jSkill_SkillInfo> Skills { get; } = [];
    }
}