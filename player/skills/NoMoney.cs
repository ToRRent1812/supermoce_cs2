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
            
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                CCSPlayerController? victimPlayer = @event.Userid;
                CCSPlayerController? attackerPlayer = @event.Attacker;

                if (attackerPlayer == victimPlayer) return HookResult.Continue;

                if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid) return HookResult.Continue;

                var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);

                if (attackerInfo?.Skill == skillName)
                {
                    var moneyServices = victimPlayer?.InGameMoneyServices;

                    if (moneyServices == null) return HookResult.Continue;

                    moneyServices.Account = 0;
                    if (victimPlayer != null)
                    {
                        Utilities.SetStateChanged(victimPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
                    }
                }
                    

                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#4c56e4", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
