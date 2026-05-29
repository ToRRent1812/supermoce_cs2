using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class OneShot : ISkill
    {
        private const Skills skillName = Skills.OneShot;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Jednostrzałowiec", "Zabijasz jednym strzałem", "#ff5CD9");
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

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && attacker.PawnIsAlive)
                info.Damage = 1000f;

            return HookResult.Continue;
        }
    }
}