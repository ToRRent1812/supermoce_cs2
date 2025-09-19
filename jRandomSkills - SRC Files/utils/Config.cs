using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class Config
    {
        private static readonly string configPath = Path.Combine(Instance.ModuleDirectory, "Config.json");
        private static ConfigModel config = LoadConfig();
        private static FileSystemWatcher? fileWatcher;

        public static ConfigModel LoadedConfig => config;

        public static void Initialize()
        {
            SetupFileWatcher();
        }

        private static ConfigModel LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                Instance.Logger.LogInformation("Config file does not exist. Create a new config file...");
                var defaultConfig = new ConfigModel();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            string json = File.ReadAllText(configPath);
            var config = new ConfigModel();

            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(json);
                if (root == null) return config;

                var settings = root["Settings"];
                if (settings != null) JsonConvert.PopulateObject(settings.ToString(), config.Settings);

                var skillsArray = (JArray?)root["SkillsInfo"];
                if (skillsArray != null)
                    foreach (var skillObj in skillsArray)
                    {
                        var name = skillObj["Name"];
                        if (name == null) continue;
                        var instance = config.SkillsInfo.FirstOrDefault(x => x.Name == name.ToString());
                        if (instance != null) JsonConvert.PopulateObject(skillObj.ToString(), instance);
                    }
            }
            catch
            {
                Instance.Logger.LogError("Error when loading the config file.");
            }

            return config;
        }

        public static void SaveConfig(ConfigModel config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                Instance.Logger.LogError("Error when saving the config file.");
            }
        }

        private static void SetupFileWatcher()
        {
            string? path = Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(path)) return;
            fileWatcher = new FileSystemWatcher(path)
            {
                Filter = Path.GetFileName(configPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            fileWatcher.Changed += (sender, e) => config = LoadConfig();
            fileWatcher.EnableRaisingEvents = true;
        }

        public static T GetValue<T>(object skill, string key)
        {
            var skillConfig = config.SkillsInfo.FirstOrDefault(s => s.Name == skill.ToString());
            if (skillConfig == null) return default!;

            var prop = skillConfig.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                var value = prop.GetValue(skillConfig);
                if (value == null) return default!;
                else return (T)Convert.ChangeType(value, typeof(T));
            }

            var field = skillConfig.GetType().GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                var value = field.GetValue(skillConfig);
                if (value == null) return default!;
                else return (T)Convert.ChangeType(value, typeof(T));
            }

            return default!;
        }

        public class ConfigModel
        {
            public Settings Settings { get; set; } = new Settings();
            public DefaultSkillInfo[] SkillsInfo { get; set; }

            public ConfigModel()
            {
                SkillsInfo = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => typeof(DefaultSkillInfo).IsAssignableFrom(t) && t.Name == "SkillConfig")
                    .Select(t =>
                    {
                        var ctor = t.GetConstructors().FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
                        if (ctor == null) return null;
                        var args = ctor.GetParameters().Select(p => Type.Missing).ToArray();
                        return ctor.Invoke(args) as DefaultSkillInfo;
                    })
                    .Where(instance => instance != null)
                    .ToArray()!;
            }
        }

        public class Settings
        {
            public string LangCode { get; set; }
            public int GameMode { get; set; }
            public bool KillerSkillInfo { get; set; }
            public bool TeamMateSkillInfo { get; set; }
            public bool SummaryAfterTheRound { get; set; }
            public bool DebugMode { get; set; }
            //public string? AlternativeSkillButton { get; set; }
            public float SkillTimeBeforeStart { get; set; }
            public float SkillDescriptionDuration { get; set; }
            public NormalCommand SetSkillCommands { get; set; }
            public NormalCommand SkillsListCommands { get; set; }
            public NormalCommand UseSkillCommands { get; set; }
            public NormalCommand HealCommands { get; set; }
            public NormalCommand ConsoleCommands { get; set; }
            public NormalCommand SetStaticSkillCommands { get; set; }
            public VotingCommand StartGameCommands { get; set; }
            public VotingCommand ChangeMapCommands { get; set; }
            public VotingCommand SwapCommands { get; set; }
            public VotingCommand ShuffleCommands { get; set; }
            public VotingCommand PauseCommands { get; set; }
            public VotingCommand SetScoreCommands { get; set; }

            public Settings()
            {
                LangCode = "pl";
                GameMode = (int)GameModes.Normal;
                KillerSkillInfo = true;
                TeamMateSkillInfo = true;
                SummaryAfterTheRound = false;
                DebugMode = true;
                //AlternativeSkillButton = "Inspect";
                SkillTimeBeforeStart = 4;
                SkillDescriptionDuration = 0;

                SetSkillCommands = new NormalCommand("ustawskill, ustaw_skill, setskill, set_skill, definirhabilidade, configurarhabilidade, 设置技能, 配置技能", "@jRandmosSkills/admin");
                SkillsListCommands = new NormalCommand("supermoc, skille, listamocy, supermoce, skills, listaHabilidades, habilidades, 技能列表, 超能力列表", "@jRandmosSkills/admin");
                UseSkillCommands = new NormalCommand("t, useSkill, usarHabilidade, 技能使用, 使用技能", "@jRandmosSkills/admin");
                HealCommands = new NormalCommand("heal, ulecz, curar, tratar, 治疗, 治愈", "@jRandmosSkills/admin");
                ConsoleCommands = new NormalCommand("console, sv, 控制台, 服务器", "@jRandmosSkills/root");
                SetStaticSkillCommands = new NormalCommand("ustawstatycznyskill, ustaw_statyczny_skill, setstaticskill, set_static_skill", "@jRandmosSkills/admin");

                StartGameCommands = new VotingCommand(true, "start, go, começar, iniciar, 开始, 启动", "@jRandmosSkills/admin", 15, 60, 15, 500, 2);
                ChangeMapCommands = new VotingCommand( true, "map, mapa, changemap, zmienmape, zmienmape, mudarMapa, trocarMapa, 更换地图, 更改地图", "@jRandmosSkills/admin", 25, 90, 15, 500, 2);
                SwapCommands = new VotingCommand(true, "swap, zmiana, trocar, 交换, 切换", "@jRandmosSkills/admin", 15, 90, 15, 20, 2);
                ShuffleCommands = new VotingCommand(true, "shuffle, embaralhar, 随机排序, 洗牌", "@jRandmosSkills/admin", 15, 90, 15, 20, 2);
                PauseCommands = new VotingCommand(true, "pause, unpause, pausar, despausar, 暂停, 恢复", "@jRandmosSkills/admin", 15, 60, 15, 2, 2);
                SetScoreCommands = new VotingCommand(true, "setscore, wynik, definirPontuacao, configurarPontos, 设置分数, 调整分数", "@jRandmosSkills/root", 15, 90, 15, 90, 2);
            }

        }

        public class NormalCommand(string alias, string permissions)
        {
            public string Alias { get; set; } = alias;
            public string Permissions { get; set; } = permissions;
        }

        public class VotingCommand(bool enableVoting, string alias, string permissions, float timeToVote, float percentagesToSuccess, float timeToNextVoting, float timeToNextSameVoting, int minimumPlayersToStartVoting) : NormalCommand(alias, permissions)
        {
            public bool EnableVoting { get; set; } = enableVoting;
            public float TimeToVote { get; set; } = timeToVote;
            public float PercentagesToSuccess { get; set; } = percentagesToSuccess;
            public float TimeToNextVoting { get; set; } = timeToNextVoting;
            public float TimeToNextSameVoting { get; set; } = timeToNextSameVoting;
            public int MinimumPlayersToStartVoting { get; set; } = minimumPlayersToStartVoting;
        }

        public class DefaultSkillInfo(Skills skill, bool active = true, string color = "#ffffff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false)
        {
            public bool NeedsTeammates { get; set; } = needsTeammates;
            public int OnlyTeam { get; set; } = (int)onlyTeam;
            public string Color { get; set; } = color;
            public bool Active { get; set; } = active;
            public string Name { get; set; } = skill.ToString();
        }

        public enum GameModes
        {
            Normal = 0,
            TeamSkills = 1,
            SameSkills = 2,
            NoRepeat = 3,
            Debug = 4
        }
    }
}