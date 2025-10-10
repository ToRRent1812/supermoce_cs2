using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RefillOnKill : ISkill
    {
        private const Skills skillName = Skills.RefillOnKill;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Rusznikarz", "Odnawiasz amunicję i zdrowie po każdym zabójstwie.", "#18dda2");
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var killer = @event.Attacker;
            var victim = @event.Userid;
            if (killer == null || killer == victim || Instance?.IsPlayerValid(killer) == false || !killer.PawnIsAlive) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == killer.SteamID);
            if (playerInfo?.Skill == skillName)
            {
                var killerPawn = killer.PlayerPawn.Value;
                var pawn = killer.Pawn?.Value;
                var weapon = pawn?.WeaponServices?.ActiveWeapon?.Value;
                if (weapon != null && weapon.IsValid)
                    weapon.Clip1 += 100;
                SkillUtils.AddHealth(killerPawn, 100);
            }
        }
    }
}
