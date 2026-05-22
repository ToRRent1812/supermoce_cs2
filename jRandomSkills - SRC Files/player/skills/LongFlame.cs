using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class LongFlame : ISkill
    {
        private const Skills skillName = Skills.LongFlame;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ognisko", "Wszystkie mołotowy na serwerze palą się 60 sek.", "#ff5e00");
        }

        public static void EnableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("inferno_flame_lifetime 60");
        }

        public static void DisableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("sv_cheats 0");
            Server.ExecuteCommand("inferno_flame_lifetime 7");
        }
    }
}