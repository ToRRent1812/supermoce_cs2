using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Gambler : ISkill
    {
        private const Skills skillName = Skills.Gambler;
        private static readonly int refreshPrice = Config.GetValue<int>(skillName, "refreshPrice");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                player.PrintToChat($" {ChatColors.Red}{Localization.GetTranslation("areareaper_used_info")}");
                return;
            }

            var skill = SkillData.Skills.FirstOrDefault(s => s.Name.Equals(commands[0], StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(commands[0], StringComparison.OrdinalIgnoreCase));
            if (skill == null)
            {
                player.PrintToChat($" {ChatColors.Red}" + Localization.GetTranslation("skill_not_found_setskill"));
                return;
            }

            if (skill.Skill == skillName)
                TakeMoney(player);
            Instance.AddTimer(.1f, () =>
            {
                playerInfo.Skill = skill.Skill;
                if (skill.Skill != skillName)
                    playerInfo.SpecialSkill = skillName;
                playerInfo.SkillChance = 1;
                Instance.SkillAction(skill.Skill.ToString(), "EnableSkill", [player]);
            });
        }

        private static void TakeMoney(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.InGameMoneyServices == null) return;
            var account = player.InGameMoneyServices.Account;
            player.InGameMoneyServices.Account = Math.Max(0, account - refreshPrice);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var skills = GetSkills(player);
            var firstSkill = skills[Instance.Random.Next(skills.Count)];
            skills.Remove(firstSkill);
            var secondSkill = skills[Instance.Random.Next(skills.Count)];

            HashSet<(string, string)> menuItems = [(firstSkill.Name, firstSkill.Skill.ToString()),
                                                   (secondSkill.Name, secondSkill.Skill.ToString()),
                                                    (Localization.GetTranslation("gambler_more", refreshPrice), skillName.ToString())];
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

            List<jSkill_SkillInfo> skillList = new(SkillData.Skills);
            skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

            if (Utilities.GetPlayers().FindAll(p => p.Team == player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).Count == 1)
            {
                Config.DefaultSkillInfo[] skillsNeedsTeammates = Config.LoadedConfig.SkillsInfo.Where(s => s.NeedsTeammates).ToArray();
                skillList.RemoveAll(s => skillsNeedsTeammates.Any(s2 => s2.Name == s.Skill.ToString()));
            }

            if (player.Team == CsTeam.Terrorist)
                skillList.RemoveAll(s => Event.counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
            else
                skillList.RemoveAll(s => Event.terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));

            return skillList.Count == 0 ? [Event.noneSkill] : skillList;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#7eff47", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int refreshPrice = 500) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int RefreshPrice { get; set; } = refreshPrice;
        }
    }
}