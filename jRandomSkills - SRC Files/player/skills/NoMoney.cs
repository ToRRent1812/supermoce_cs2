using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class NoMoney : ISkill
    {
        private const Skills skillName = Skills.NoMoney;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Komornik", "Zabij wroga, by ukraść mu pieniądze", "#4c56e4");
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            CCSPlayerController? victimPlayer = @event.Userid;
            CCSPlayerController? attackerPlayer = @event.Attacker;

            if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid || attackerPlayer.TeamNum == victimPlayer.TeamNum) return;
            if (attackerPlayer == victimPlayer) return;

            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);
            if (attackerInfo?.Skill == skillName)
            {
                var vmoneyServices = victimPlayer?.InGameMoneyServices;
                var amoneyServices = attackerPlayer?.InGameMoneyServices;
                if (vmoneyServices == null || amoneyServices == null) return;

                amoneyServices!.Account += vmoneyServices!.Account;
                vmoneyServices!.Account = 0;
                Utilities.SetStateChanged(victimPlayer!, "CCSPlayerController", "m_pInGameMoneyServices");
                Utilities.SetStateChanged(attackerPlayer!, "CCSPlayerController", "m_pInGameMoneyServices");
            }
        }
    }
}
