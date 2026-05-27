using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class Shade : ISkill
    {
        private const Skills skillName = Skills.Shade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Cień", "25% szans na pojawienie się za plecami trafionego wroga", "#4d4d4d");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false) return;

            var victimInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && Instance?.Random.Next(1,5) == 1)
                TeleportAttackerBehindVictim(attacker!, victim!);
        }

        private static bool CheckTeleport(CCSPlayerController attacker, CCSPlayerController victim, Vector startPos, Vector endPos, QAngle angle)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            if (attackerPawn == null || !attackerPawn.IsValid) return false;

            var victimPawn = victim.PlayerPawn.Value;
            if (victimPawn == null || !victimPawn.IsValid) return false;

            var result = RayTrace.TraceHullShape(
                    startPos,
                    endPos,
                    victim,
                    attackerPawn.Collision.Mins,
                    attackerPawn.Collision.Maxs,
                    null,
                    null,
                    angle
                );

            if (!result.HasValue)
                return false;

            return !result.Value.DidHit;
        }

        private static void TeleportAttackerBehindVictim(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var victimPawn = victim.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            if (victimPawn == null || attackerPawn == null || victimPawn.AbsOrigin == null || victimPawn.AbsRotation == null) return;

            Vector victimPos = new(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z);
            QAngle victimAngles = new(victimPawn.AbsRotation.X, victimPawn.AbsRotation.Y, victimPawn.AbsRotation.Z);
            float distance = 100f;

            int[] angles = [0, 90, -90];

            foreach (int extraAngle in angles)
            {
                QAngle targetAngle = new(0, victimAngles.Y + extraAngle, 0);
                Vector direction = SkillUtils.GetForwardVector(targetAngle);
                Vector targetPos = victimPos - (direction * distance);

                if (CheckTeleport(attacker, victim, victimPos, targetPos, targetAngle))
                {
                    attackerPawn.Teleport(targetPos, targetAngle, Vector.Zero);
                    break;
                }
            }
        }
    }
}
