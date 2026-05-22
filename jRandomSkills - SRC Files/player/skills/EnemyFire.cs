using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Enemyfire : ISkill
    {
        private const Skills skillName = Skills.Enemyfire;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Zdrajca", "Do końca rundy włączony jest friendly fire", "#ff0000");
        }

        public static void EnableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("mp_friendlyfire 1; ff_damage_reduction_bullets 0.33; ff_damage_reduction_grenade 0.85; ff_damage_reduction_grenade_self 1; ff_damage_reduction_other 0.4; mp_autokick 0");
        }

        public static void DisableSkill(CCSPlayerController _)
        {
            Server.ExecuteCommand("mp_friendlyfire 0");
        }
    }
}