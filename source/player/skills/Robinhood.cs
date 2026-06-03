using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Supermoce.src.player;

namespace Supermoce
{
    public class Robinhood : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Robinhood;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Robin Chuj", 
            "Wybierasz gracza, który straci ekwipunek", 
            "#2fe9ab");
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
            {
                bool hasC4 = enemyPawn.WeaponServices?.MyWeapons
                    .Any(w => w?.Value?.DesignerName == "weapon_c4") == true;

                enemy.RemoveWeapons();
                SkillUtils.TryGiveWeapon(enemy, CsItem.Knife);

                if (hasC4)
                    enemy.GiveNamedItem("weapon_c4");
            }
            SkillUtils.PrintToChat(enemy, $" Wróg skasował Ci ekwipunek", true);
            if (playerInfo != null) playerInfo.SkillChance = 1;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
        }
    }
}