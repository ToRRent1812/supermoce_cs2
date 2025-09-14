using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dracula : ISkill
    {
        private const Skills skillName = Skills.Dracula;
        private static readonly float healthRegainScale = Config.GetValue<float>(skillName, "healthRegainScale");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
                HealAttacker(attacker!, @event.DmgHealth);
        }

        private static void HealAttacker(CCSPlayerController attacker, float damage)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            if (attackerPawn == null) return;

            int newHealth = (int)(attackerPawn.Health + (damage * healthRegainScale));

            attackerPawn.MaxHealth = Math.Max(newHealth, 100);
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iMaxHealth");

            attackerPawn.Health = newHealth;
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FA050D", CsTeam onlyTeam = CsTeam.Terrorist, bool needsTeammates = false, float healthRegainScale = .4f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float HealthRegainScale { get; set; } = healthRegainScale;
        }
    }
}