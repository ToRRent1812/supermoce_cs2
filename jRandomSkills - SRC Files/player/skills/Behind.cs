using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Behind : ISkill
    {
        private const Skills skillName = Skills.Behind;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Przekręcenie", "Szansa na obrócenie wroga po trafieniu", "#00FF00");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event!.Attacker;
            var victim = @event!.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
                if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                    RotateEnemy(victim);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(2,6) * 5 ?? 10; //10-25%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }

        private static void RotateEnemy(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;

            if (pawn == null || !pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

            var currentPosition = pawn.AbsOrigin;
            var currentAngles = pawn.EyeAngles;

            QAngle newAngles = new(
                currentAngles.X,
                currentAngles.Y + 180,
                currentAngles.Z
            );

            pawn.Teleport(currentPosition, newAngles, new Vector(0, 0, 0));
        }
    }
}