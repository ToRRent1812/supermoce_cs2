using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Push : ISkill
    {
        private const Skills skillName = Skills.Push;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Śmierdziel", "Szansa na odsunięcie wroga po trafieniu", "#1e9ab0");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim)
                return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                    PushEnemy(victim, attacker!.PlayerPawn.Value!.EyeAngles);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            int randomValue = Instance?.Random?.Next(2,7) * 5 ?? 10; //10-30%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }

        private static void PushEnemy(CCSPlayerController player, QAngle attackerAngle)
        {
            if (player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            var currentPosition = player.PlayerPawn.Value.AbsOrigin;
            var currentAngles = player.PlayerPawn.Value.EyeAngles;

            Vector newVelocity = SkillUtils.GetForwardVector(attackerAngle) * 500f;
            newVelocity.Z = player.PlayerPawn.Value.AbsVelocity.Z + 200f;

            player.PlayerPawn.Value.Teleport(currentPosition, currentAngles, newVelocity);
        }
    }
}