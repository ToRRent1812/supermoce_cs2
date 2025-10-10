using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class AntyHead : ISkill
    {
        private const Skills skillName = Skills.AntyHead;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Żelazna Głowa", "Nie otrzymujesz obrażeń w głowę", "#8B4513");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            int hitgroup = @event.Hitgroup;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            if (playerInfo?.Skill == skillName && hitgroup == (int)HitGroup_t.HITGROUP_HEAD)
                ApplyIronHeadEffect(victim!, @event.DmgHealth);
        }

        private static void ApplyIronHeadEffect(CCSPlayerController victim, float damage)
        {
            var playerPawn = victim.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            var newHealth = playerPawn.Health + damage;

            if (newHealth > 100)
                newHealth = 100;

            playerPawn.Health = (int)newHealth;
        }
    }
}