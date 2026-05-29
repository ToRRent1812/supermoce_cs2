using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class TimeManipulator : ISkill
    {
        private const Skills skillName = Skills.TimeManipulator;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static int cd = 30;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Matrix", "Możesz spowolnić czasoprzestrzeń", "#2ec761");
        }
        
        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(3, 11)) ?? 3) * 5;
            SkillPlayerInfo.Clear();
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
                SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if(player == null || !player.IsValid || !player.PawnIsAlive) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                        UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.IsValid || !player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;

                    Server.ExecuteCommand("sv_cheats 1");
                    Server.ExecuteCommand("host_timescale 0.25");
                    Instance?.AddTimer(.9f, () =>
                    {
                        Server.ExecuteCommand("host_timescale 1");
                        Server.ExecuteCommand("sv_cheats 0");
                    });
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
        }
    }
}