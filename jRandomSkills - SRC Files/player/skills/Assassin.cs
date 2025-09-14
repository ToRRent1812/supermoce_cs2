﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Assassin : ISkill
    {
        private const Skills skillName = Skills.Assassin;
        private static readonly float damageMultiplier = Config.GetValue<float>(skillName, "damageMultiplier");
        private static readonly float toleranceDeg = Config.GetValue<float>(skillName, "toleranceDeg");
        private static readonly string[] nades = ["inferno", "flashbang", "smokegrenade", "decoy", "hegrenade"];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var damage = @event.DmgHealth;
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            var weapon = @event.Weapon;
            HitGroup_t hitgroup = (HitGroup_t)@event.Hitgroup;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            if (nades.Contains(weapon)) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (IsBehind(attacker!, victim!))
                SkillUtils.TakeHealth(victim!.PlayerPawn.Value, (int)(damage * (damageMultiplier - 1f)));
        }

        private static bool IsBehind(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            var victimPawn = victim.PlayerPawn.Value;
            if (attackerPawn == null || !attackerPawn.IsValid || victimPawn == null || !victimPawn.IsValid) return false;
            if (victimPawn.AbsRotation == null || attackerPawn.AbsRotation == null) return false;
            var angles = GetAngleRange(victimPawn.AbsRotation.Y);
            return IsBeetween(angles.Item1, angles.Item2, attackerPawn.AbsRotation.Y);
        }

        private static (float, float) GetAngleRange(float angle)
        {
            float min = angle - toleranceDeg;
            float max = angle + toleranceDeg;

            if (min < -180) min += 360f;
            if (max > 180f) max -= 360f;

            return (min, max);
        }

        private static bool IsBeetween(float a, float b, float target)
        {
            if (a <= b)
                return target >= a && target <= b;
            return target >= a || target <= b;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#d9d9d9", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float damageMultiplier = 2f, float toleranceDeg = 65f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float DamageMultiplier { get; set; } = damageMultiplier;
            public float ToleranceDeg { get; set; } = toleranceDeg;
        }
    }
}