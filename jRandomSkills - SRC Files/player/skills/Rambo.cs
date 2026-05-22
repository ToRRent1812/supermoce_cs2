using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Rambo : ISkill
    {
        private const Skills skillName = Skills.Rambo;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Rambo", "Bonusowe HP w rundzie", "#009905");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            int healthBonus = Instance?.Random.Next(100, 501) ?? 100;
            SkillUtils.AddHealth(player.PlayerPawn.Value, healthBonus, 600);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            ResetHealth(player);
        }

        public static void ResetHealth(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            pawn.MaxHealth = 100;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            pawn.Health = Math.Min(pawn.Health, 100);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }
}
