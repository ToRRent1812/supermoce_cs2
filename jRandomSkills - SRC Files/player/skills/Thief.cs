using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Thief : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Thief;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Złodziej", 
            "Możesz ukraść supermoc 1 wroga", 
            "#adaec7");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.ValidateSkillUse(player, skillName, out var playerInfo))
                return;

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
            MenuSkillFramework.OnSkillEnabled(skillName, player);

            SkillUtils.CreateTargetingMenu(
                player,
                enemy =>
                {
                    var enemyInfo = SkillUtils.GetPlayerInfo(enemy);
                    var skillData = enemyInfo == null ? null : SkillData.Skills.FirstOrDefault(s => s.Skill == enemyInfo.Skill);
                    return skillData != null && skillData.TeamNumber == 0;
                },
                null,
                () => Event.SetRandomSkill(player));
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
        }

        private static void StealSkill(CCSPlayerController player, CCSPlayerController enemy)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            var enemyInfo = SkillUtils.GetPlayerInfo(enemy);
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
                SkillUtils.PrintToChat(enemy, $"Wróg ukradł Twoją supermoc");
            });
        }
    }
}