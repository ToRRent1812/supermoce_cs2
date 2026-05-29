using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Catapult : ISkill
    {
        private const Skills skillName = Skills.Catapult;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Katapulta", "Szansa na podrzucenie wroga po trafieniu", "#FF4500");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && victim!.PawnIsAlive)
                if (Instance?.Random.NextDouble() <= attackerInfo.SkillChance)
                {
                    var victimPawn = victim.PlayerPawn?.Value;
                    if (victimPawn != null)
                        victimPawn.AbsVelocity.Z = 350f;
                }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(2,6) * 5 ?? 10; //10-25%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }
    }
}