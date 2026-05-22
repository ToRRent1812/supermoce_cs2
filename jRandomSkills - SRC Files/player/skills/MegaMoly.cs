using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class MegaMoly : ISkill
    {
        private const Skills skillName = Skills.MegaMoly;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Mega-tov", "Wszystkie mołotowy na serwerze są 5 razy większe", "#ff0000");
        }

        public static void EnableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("inferno_max_range 750; inferno_max_flames 80");
        }

        public static void DisableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("sv_cheats 0");
            Server.ExecuteCommand("inferno_max_range 150; inferno_max_flames 16");
        }
    }
}