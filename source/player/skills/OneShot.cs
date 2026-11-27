using CounterStrikeSharp.API.Core;
using Supermoce.src.player;

namespace Supermoce
{
    public class OneShot : ISkill
    {
        private const Skills skillName = Skills.OneShot;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Jednostrzałowiec", 
            "Zabijasz jednym strzałem", 
            "#ff5CD9");
        }

        public static HookResult OnPlayerTakeDamage(CCSPlayerPawn victim, CTakeDamageInfo info)
        {
            if (victim == null || !victim.IsValid || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);

            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null || victim.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && attacker.PawnIsAlive)
                info.Damage = 1000f;

            return HookResult.Continue;
        }
    }
}