using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Duplicator : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Duplicator;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Plagiator", 
            "Kopiujesz 1 supermoc innego gracza", 
            "#ffb73b");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var target, notFoundMsg: "Nie znaleziono gracza o takim ID."))
                return;

            DuplicateSkill(player, target!);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);

            SkillUtils.CreateTargetingMenu(
                player,
                target =>
                {
                    var targetInfo = SkillUtils.GetPlayerInfo(target);
                    var skillData = targetInfo == null ? null : SkillData.Skills.FirstOrDefault(s => s.Skill == targetInfo.Skill);
                    return skillData != null && skillData.TeamNumber == 0;
                },
                "Nie znaleziono gracza z uniwersalną supermocą.");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
        }

        private static void DuplicateSkill(CCSPlayerController player, CCSPlayerController target)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            var targetInfo = SkillUtils.GetPlayerInfo(target);
            if (playerInfo == null || targetInfo == null) return;

            var targetSkill = targetInfo.Skill;

            SkillUtils.CloseMenu(player);
            Instance?.AddTimer(.1f, () =>
            {
                playerInfo.Skill = targetSkill;
                playerInfo.SpecialSkill = skillName;
                SkillUtils.CloseMenu(player);
                Instance.SkillAction(targetSkill.ToString(), "EnableSkill", [player]);
            });
        }
    }
}