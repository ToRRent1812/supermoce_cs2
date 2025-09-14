using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Medic : ISkill
    {
        private const Skills skillName = Skills.Medic;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if(player == null || !player.IsValid || !player.PawnIsAlive) continue;
                player.RemoveItemByDesignerName("weapon_healthshot");
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid || !player.PawnIsAlive) return;
            int healthshot = Instance.Random.Next(3, 6);
            SkillUtils.TryGiveWeapon(player, CsItem.Healthshot, healthshot);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid || !player.PawnIsAlive) return;
            player.RemoveItemByDesignerName("weapon_healthshot");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#10c212", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}