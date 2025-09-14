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
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var killer = @event.Attacker;
            if (killer == null || !Instance.IsPlayerValid(killer) || !killer.PawnIsAlive) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == killer.SteamID);
            if (playerInfo?.Skill == skillName)
            {
                var pawn = killer.Pawn?.Value;
                var weapon = pawn?.WeaponServices?.ActiveWeapon?.Value;
                if (weapon != null && weapon.IsValid)
                    weapon.Clip1 += 100;
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#18dda2", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
