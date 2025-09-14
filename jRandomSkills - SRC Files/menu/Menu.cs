using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using static jRandomSkills.jRandomSkills;
using System.Text.RegularExpressions;
using jRandomSkills.src.utils;

namespace jRandomSkills
{
    public static class Menu
    {
        public static void DisplaySkillsList(CCSPlayerController player)
        {
            CenterHtmlMenu menu = new($"[ ★ {Localization.GetTranslation("skills_menu")} ★ ]");
            
            for (int i = 0; i < SkillData.Skills.Count; i++)
            {
                var skill = SkillData.Skills[i];
                string skillName = $"{skill.Name}";
                menu.AddMenuOption($" ★ {skillName}", (player, option) =>
                {
                    string selectedSkillName = option.Text;
                    string pattern = "\\[/?color\\b[^\\]]*\\]";
                    string cleanSkillName = Regex.Replace(selectedSkillName, pattern, "");
                    string skillName = cleanSkillName.Replace($" ★ ", "");
                    int intValue = Array.IndexOf(SkillData.Skills.Select(r => r.Name).ToArray(), skillName);
                    string skillDesc = SkillData.Skills[intValue].Description;
                    SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{skillName}{ChatColors.Lime}: {skillDesc}", false);
                    MenuManager.CloseActiveMenu(player);
                });
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
    }
}