using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using System.Collections.Concurrent;

namespace jRandomSkills
{
    public class Dopamine : ISkill
    {
        private const Skills skillName = Skills.Dopamine;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static int cd = 25;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Dopaminka", "Możesz przyspieszyć życie na serwerze", "#FA050D");
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(4, 9)) ?? 4) * 5;
            SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName && SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                    UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player) =>
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });

        public static void DisableSkill(CCSPlayerController player) =>
            SkillPlayerInfo.TryRemove(player.SteamID, out _);

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0
                ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>"
                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            player.PrintToCenterHtml(skillLine + remainingLine);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (Instance?.GameRules?.FreezePeriod == true || player?.IsValid != true) return;
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;

                    Server.ExecuteCommand("sv_cheats 1");
                    Server.ExecuteCommand("host_timescale 2");
                    Instance?.AddTimer(4.5f, () =>
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