using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Evade : ISkill
    {
        private const Skills skillName = Skills.Evade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "CS2", "25% szans na uniknięcie trafienia", "#b2dd18");
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(victim.SteamID, out var skillPlayer) ? skillPlayer : null;
            if (playerInfo == null) return HookResult.Continue;
            if (playerInfo?.Skill == skillName && victim.PawnIsAlive && Instance?.Random.Next(1, 5) == 1)
            {
                info.Damage = 0f;
            }

            return HookResult.Continue;
        }
    }
}