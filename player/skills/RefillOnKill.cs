using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RefillOnKill : ISkill
    {
        private const Skills skillName = Skills.RefillOnKill;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var killer = @event.Attacker;
                if (killer == null || !Instance.IsPlayerValid(killer)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == killer.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    var weapon = killer.Pawn.Value.WeaponServices?.ActiveWeapon.Value;
                    if (weapon != null && weapon.IsValid)
                    {
                        weapon.Clip1 += 100;
                    }
                }
                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#18dda2", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
