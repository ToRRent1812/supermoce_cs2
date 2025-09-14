using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class NoMoney : ISkill
    {
        private const Skills skillName = Skills.NoMoney;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            CCSPlayerController? victimPlayer = @event.Userid;
            CCSPlayerController? attackerPlayer = @event.Attacker;

            if (attackerPlayer == victimPlayer) return;
            if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid) return;

            var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);
            if (attackerInfo?.Skill == skillName)
            {
                var moneyServices = victimPlayer?.InGameMoneyServices;
                if (moneyServices == null) return;

                moneyServices.Account = 0;
                Utilities.SetStateChanged(victimPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#4c56e4", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
