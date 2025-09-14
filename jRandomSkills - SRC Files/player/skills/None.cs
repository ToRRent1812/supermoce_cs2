using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class None : ISkill
    {
        private const Skills skillName = Skills.None;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FFFFFF", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}