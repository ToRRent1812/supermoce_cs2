using CounterStrikeSharp.API.Core;
using Supermoce.src.player;

namespace Supermoce
{
    public class Realism : ISkill
    {
        private const Skills skillName = Skills.Realism;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Realizm", 
            "Każda broń w twoich rękach zabija 1 strzałem w głowę", 
            "#30c538");
        }
        
        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null || playerInfo.Skill != skillName) return HookResult.Continue;

            if (info.GetHitGroup() == HitGroup_t.HITGROUP_HEAD || info.GetHitGroup() == HitGroup_t.HITGROUP_NECK)
                info.Damage = 999.0f;

            return HookResult.Continue;
        }
    }
}