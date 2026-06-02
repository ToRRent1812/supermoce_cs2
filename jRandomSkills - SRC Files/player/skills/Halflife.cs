using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Halflife : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Halflife;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Half-Life", 
            "Wybierasz gracza, który straci połowę zdrowia", 
            "#e6b821");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy))
                return;

            var enemyPawn = enemy!.PlayerPawn?.Value;
            if (enemyPawn != null && enemyPawn.IsValid)
                SkillUtils.TakeHealth(enemyPawn, enemyPawn.Health / 2);

            SkillUtils.PrintToChat(enemy, $"Wróg usunął Ci połowę zdrowia");
            if (playerInfo != null) playerInfo.SkillChance = 1;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            SkillUtils.DestroyTargetingSkill(player);
        }
    }
}