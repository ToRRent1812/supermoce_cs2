using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class None : ISkill
    {
        private const Skills skillName = Skills.None;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Inwalida", "Nie posiadasz supermocy", "#FFFFFF");
        }
    }
}