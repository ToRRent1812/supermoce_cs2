using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;

using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Duplicator : ISkill
    {
        private const Skills skillName = Skills.Duplicator;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Plagiator", "Kopiujesz 1 supermoc innego gracza", "#ffb73b");
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if (!SkillUtils.HasMenu(player)) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p != player && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
                ConcurrentBag<(string, string)> menuItems = [];
                foreach (var enemy in enemies)
                {
                    var enemyInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
                    if (enemyInfo == null) continue;
                    var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                    if (skillData == null) continue;
                    if (skillData.TeamNumber != 0) continue;
                    menuItems.Add(($"{skillData.Name}", enemy.Index.ToString()));
                }
                SkillUtils.UpdateMenu(player, menuItems);
            }
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

            DuplicateSkill(player, enemy);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p != player && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
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
                    menuItems.Add(($"{skillData.Name}", enemy.Index.ToString()));
                }

                if (menuItems.Count > 0)
                    SkillUtils.CreateMenu(player, menuItems);
                else
                    player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza z uniwersalną supermocą.");
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

        private static void DuplicateSkill(CCSPlayerController player, CCSPlayerController enemy)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            var enemyInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
            if (playerInfo == null || enemyInfo == null) return;

            var enemySkill = enemyInfo.Skill;
            bool ctSkill = SkillData.Skills.Any(s => s.Skill == enemySkill && s.TeamNumber == 2);
            bool ttSkill = SkillData.Skills.Any(s => s.Skill == enemySkill && s.TeamNumber == 1);

            if ((player.Team == CsTeam.Terrorist && ctSkill) || (player.Team == CsTeam.CounterTerrorist && ttSkill) || enemySkill == playerInfo.Skill)
            {
                Instance?.AddTimer(.1f, () =>
                {
                    Instance.SkillAction(skillName.ToString(), "EnableSkill", [player]);
                    player.PrintToChat($" {ChatColors.Red}Ta supermoc nie działa w tej drużynie!");
                });
                return;
            }

            SkillUtils.CloseMenu(player);
            Instance?.AddTimer(.1f, () =>
            {
                playerInfo.Skill = enemySkill;
                playerInfo.SpecialSkill = skillName;
                SkillUtils.CloseMenu(player);
                Instance.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);
            });
        }
    }
}
