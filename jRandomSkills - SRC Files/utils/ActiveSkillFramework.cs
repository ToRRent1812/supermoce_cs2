using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public static class ActiveSkillFramework
    {
        private static readonly ConcurrentDictionary<(Skills, ulong), ActiveSkillState> _skillStates = [];
        private static readonly ConcurrentDictionary<ulong, string> _lastHudStrings = [];

        public class ActiveSkillState
        {
            public ulong SteamID { get; set; }
            public DateTime LastUseTime { get; set; }
            public int CooldownSeconds { get; set; }
            public bool CanUse { get; set; }
        }

        public static void OnSkillEnabled(Skills skill, CCSPlayerController player, ActiveSkillConfig config)
        {
            int cooldown = config.GenerateCooldown();
            var key = (skill, player.SteamID);
            
            _skillStates.AddOrUpdate(key,
                new ActiveSkillState
                {
                    SteamID = player.SteamID,
                    CanUse = true,
                    LastUseTime = DateTime.MinValue,
                    CooldownSeconds = cooldown
                },
                (k, existing) => new ActiveSkillState
                {
                    SteamID = player.SteamID,
                    CanUse = true,
                    LastUseTime = DateTime.MinValue,
                    CooldownSeconds = cooldown
                });
        }

        public static void OnSkillDisabled(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.SteamID);
            _skillStates.TryRemove(key, out _);
            _lastHudStrings.TryRemove(player.SteamID, out _);
        }

        public static bool CanUseSkill(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.SteamID);
            if (!_skillStates.TryGetValue(key, out var state))
                return false;

            var timeElapsed = (int)(DateTime.Now - state.LastUseTime).TotalSeconds;
            return timeElapsed >= state.CooldownSeconds;
        }

        public static bool TryGetSkillState(Skills skill, CCSPlayerController player, out ActiveSkillState? state)
        {
            var key = (skill, player.SteamID);
            return _skillStates.TryGetValue(key, out state);
        }

        public static void MarkSkillUsed(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.SteamID);
            if (_skillStates.TryGetValue(key, out var state))
            {
                state.LastUseTime = DateTime.Now;
            }
        }

        public static void UpdateHUD(Skills skill, CCSPlayerController player, ActiveSkillConfig config)
        {
            if (player == null || !player.IsValid || player.PlayerPawn?.Value == null) 
                return;

            var key = (skill, player.SteamID);
            if (!_skillStates.TryGetValue(key, out var state))
                return;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skill);
            if (skillData == null) 
                return;

            var timeElapsed = (int)(DateTime.Now - state.LastUseTime).TotalSeconds;
            var remainingCooldown = Math.Max(state.CooldownSeconds - timeElapsed, 0);

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillData.Description}</font>";
            
            string remainingLine;
            if (remainingCooldown > 0)
            {
                remainingLine = $"<br><font class='fontSize-m' color='#FFE3D6'>Poczekaj <font color='#FFA600'>{remainingCooldown}</font> sek.</font>";
            }
            else
            {
                remainingLine = $"<br><font class='fontSize-s' class='fontWeight-Bold' color='#DEFF24'>Wciśnij INSPEKT by użyć</font>";
            }

            var hudContent = skillLine + remainingLine;
            PrintCachedHud(player, hudContent);
        }

        public static void PrintCachedHud(CCSPlayerController player, string hudContent)
        {
            if (player == null || !player.IsValid) return;

            bool rebuildStrings = (Server.TickCount & 7) == 0;
            if (!_lastHudStrings.TryGetValue(player.SteamID, out var lastHud) || lastHud != hudContent || rebuildStrings)
            {
                _lastHudStrings.AddOrUpdate(player.SteamID, hudContent, (k, v) => hudContent);
            }
            player.PrintToCenterHtml(hudContent);
        }

        public static void OnNewRound()
        {
            _skillStates.Clear();
            _lastHudStrings.Clear();
        }
    }
}
