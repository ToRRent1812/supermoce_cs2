﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Thief : ISkill
    {
        private const Skills skillName = Skills.Thief;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if (!SkillUtils.HasMenu(player)) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
                if (enemies.Length > 0)
                {
                    HashSet<(string, string)> menuItems = [];
                    foreach (var enemy in enemies)
                    {
                        var enemyInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
                        if (enemyInfo == null) continue;
                        var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                        if (skillData == null) continue;
                        menuItems.Add(($"{enemy.PlayerName} : {skillData.Name}", enemy.Index.ToString()));
                    }
                    SkillUtils.CreateMenu(player, menuItems);
                }
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

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Index.ToString() == enemyId);

            if (enemy == null)
            {
                player.PrintToChat($" {ChatColors.Red}" + Localization.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            StealSkill(player, enemy);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                HashSet<(string, string)> menuItems = [];
                foreach (var enemy in enemies)
                {
                    var enemyInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
                    if (enemyInfo == null) continue;
                    var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                    if (skillData == null) continue;
                    menuItems.Add(($"{enemy.PlayerName} : {skillData.Name}", enemy.Index.ToString()));
                }
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                player.PrintToChat($" {ChatColors.Red}{Localization.GetTranslation("selectplayerskill_incorrect_enemy_index")}");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
            SkillUtils.CloseMenu(player);
        }

        private static void StealSkill(CCSPlayerController player, CCSPlayerController enemy)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            var enemyInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == enemy.SteamID);
            if (playerInfo == null || enemyInfo == null) return;
            var enemySkill = enemyInfo.Skill;

            if (playerInfo != null)
            {
                Instance.AddTimer(.1f, () =>
                {
                    playerInfo.Skill = enemySkill;
                    playerInfo.SpecialSkill = skillName;
                    Instance.SkillAction(enemySkill.ToString(), "EnableSkill", [player]);
                    player.PrintToChat($" {ChatColors.Green}" + Localization.GetTranslation("thief_player_info", enemy.PlayerName));
                });
            }

            if (enemyInfo != null)
            {
                Instance.AddTimer(.1f, () =>
                {
                    Instance.SkillAction(enemySkill.ToString(), "DisableSkill", [enemy]);
                    enemyInfo.SpecialSkill = enemySkill;
                    enemyInfo.Skill = Skills.None;
                    enemy.PrintToChat($" {ChatColors.Red}" + Localization.GetTranslation("thief_enemy_info"));
                });
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#adaec7", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}