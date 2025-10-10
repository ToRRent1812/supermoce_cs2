using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Gambler : ISkill
    {
        private const Skills skillName = Skills.Gambler;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Polityk", "Wybierasz sobie supermoc", "#7eff47");
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                player.PrintToChat($" {ChatColors.Red}Twoja moc została już wykorzystana.");
                return;
            }

            var skill = SkillData.Skills.FirstOrDefault(s => s.Name.Equals(commands[0], StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(commands[0], StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono takiej supermocy.");
                return;
            }
            Instance?.AddTimer(.1f, () =>
            {
                playerInfo.Skill = skill.Skill;
                if (skill.Skill != skillName)
                    playerInfo.SpecialSkill = skillName;
                playerInfo.SkillChance = 1;
                Instance?.SkillAction(skill.Skill.ToString(), "EnableSkill", [player]);
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var skills = GetSkills(player);
            var firstSkill = skills[(Instance?.Random.Next(skills.Count)) ?? 0];
            skills.Remove(firstSkill);
            var secondSkill = skills[(Instance?.Random.Next(skills.Count)) ?? 0];
            skills.Remove(secondSkill);
            var thirdSkill = skills[(Instance?.Random.Next(skills.Count)) ?? 0];

            ConcurrentBag<(string, string)> menuItems = [(firstSkill.Name, firstSkill.Skill.ToString()),
                                                   (secondSkill.Name, secondSkill.Skill.ToString()),
                                                    (thirdSkill.Name, thirdSkill.Skill.ToString())];
            SkillUtils.CreateMenu(player, menuItems);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillUtils.CloseMenu(player);
        }

        private static List<jSkill_SkillInfo> GetSkills(CCSPlayerController player)
        {
            var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (skillPlayer == null) return [Event.noneSkill];

            List<jSkill_SkillInfo> skillList = [.. SkillData.Skills];
            skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

            if (player.Team == CsTeam.Terrorist)
                skillList.RemoveAll(s => Event.counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
            else
                skillList.RemoveAll(s => Event.terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));

            return skillList.Count == 0 ? [Event.noneSkill] : skillList;
        }
    }
}