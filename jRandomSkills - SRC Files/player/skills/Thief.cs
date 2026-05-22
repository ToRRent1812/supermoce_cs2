using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Thief : ISkill
    {
        private const Skills skillName = Skills.Thief;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Złodziej", "Możesz ukraść supermoc wybranemu graczowi", "#adaec7");
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if (!SkillUtils.HasMenu(player)) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
                ConcurrentBag<(string, string)> menuItems = [];
                foreach (var enemy in enemies)
                {
                    var enemyInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
                    if (enemyInfo == null) continue;
                    var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                    if (skillData == null) continue;
                    if (skillData.TeamNumber != 0) continue;
                    menuItems.Add(($"{enemy.PlayerName}", enemy.Index.ToString()));
                }
                    SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Index.ToString() == enemyId);

            if (enemy == null)
            {
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
                return;
            }

            StealSkill(player, enemy);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                ConcurrentBag<string> skills = [];
                ConcurrentBag<(string, string)> menuItems = [];
                foreach (var enemy in enemies)
                {
                    var enemyInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
                    if (enemyInfo == null) continue;
                    var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                    if (skillData == null) continue;
                    if (skillData.TeamNumber != 0) continue;
                    skills.Add(skillData.Skill.ToString());
                    menuItems.Add(($"{enemy.PlayerName}", enemy.Index.ToString()));
                }
                int ctSkills = SkillData.Skills.Count(s => skills.Contains(s.Name) && s.TeamNumber == 2);
                int ttSkills = SkillData.Skills.Count(s => skills.Contains(s.Name) && s.TeamNumber == 1);
                if ((player.Team == CsTeam.Terrorist && ctSkills == skills.Count) || (player.Team == CsTeam.CounterTerrorist && ttSkills == skills.Count))
                {
                    Event.SetRandomSkill(player);
                    return;
                }
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
            SkillUtils.CloseMenu(player);
        }

        private static void StealSkill(CCSPlayerController player, CCSPlayerController enemy)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            var enemyInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
            if (playerInfo == null || enemyInfo == null) return;
            var enemySkill = enemyInfo.Skill;

            SkillUtils.CloseMenu(player);
            Instance?.AddTimer(.1f, () =>
            {
                playerInfo.Skill = enemySkill;
                playerInfo.SpecialSkill = skillName;
                SkillUtils.CloseMenu(player);
                Instance?.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);
            });

            Instance?.AddTimer(.1f, () =>
            {
                Instance?.SkillAction(enemySkill.ToString(), "DisableSkill", [enemy]);
                enemyInfo.SpecialSkill = enemySkill;
                enemyInfo.Skill = Skills.None;
                enemyInfo.RandomPercentage = "";
                SkillUtils.PrintToChat(enemy, $"Twoja supermoc została skradziona.", true);
            });
        }
    }
}
