using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Deactivator : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Deactivator;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Antidotum", 
            "Pozbawiasz 1 wroga supermocy", 
            "#919191");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var _, out var enemy))
                return;

            DeactivateSkill(player, enemy!);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
        }

        private static void DeactivateSkill(CCSPlayerController player, CCSPlayerController enemy)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            var enemyInfo = SkillUtils.GetPlayerInfo(enemy);

            if (playerInfo != null)
            {
                playerInfo.Skill = Skills.None;
                playerInfo.SpecialSkill = skillName;
            }

            if (enemyInfo != null)
            {
                Instance?.SkillAction(enemyInfo.Skill.ToString(), "DisableSkill", [enemy]);
                enemyInfo.SpecialSkill = enemyInfo.Skill;
                enemyInfo.Skill = Skills.None;
                enemyInfo.RandomPercentage = "";
                SkillUtils.PrintToChat(enemy, $"Dostałeś antidotum. Straciłeś supermoc");
            }
        }
    }
}