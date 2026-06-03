using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Gambler : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Gambler;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(
                skillName,
                "Polityk",
                "Wybierasz sobie 1 z 3 supermocy",
                "#7eff47");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                SkillUtils.PrintToChat(player, $"Twoja moc została już wykorzystana.", true);
                return;
            }

            var skill = SkillData.Skills.FirstOrDefault(s => s.Name.Equals(commands[0], StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(commands[0], StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                SkillUtils.PrintToChat(player, $"Nie znaleziono takiej supermocy.", true);
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
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var skills = GetSkills(player);
            var chosen = new List<SkillInfo>();
            var remaining = new List<SkillInfo>(skills);

            for (int i = 0; i < 3 && remaining.Count > 0; i++)
            {
                int idx = Instance?.Random.Next(remaining.Count) ?? 0;
                chosen.Add(remaining[idx]);
                remaining.RemoveAt(idx);
            }

            ConcurrentBag<(string, string)> menuItems = [.. chosen.Select(s => (s.Name, s.Skill.ToString()))];
            SkillUtils.CreateMenu(player, menuItems);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
        }

        private static List<SkillInfo> GetSkills(CCSPlayerController player)
        {
            var skillPlayer = SkillUtils.GetPlayerInfo(player);
            if (skillPlayer == null) return [Event.noneSkill];

            List<SkillInfo> skillList = [.. SkillData.Skills];
            skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

            skillList.RemoveAll(s => s.TeamNumber != 0);
            skillList.RemoveAll(s => s.Objective != 0);

            return skillList.Count == 0 ? [Event.noneSkill] : skillList;
        }
    }
}
