using CounterStrikeSharp.API.Core;
using Supermoce.src.player;

namespace Supermoce
{
    public class Commando : ISkill
    {
        private const Skills skillName = Skills.Commando;

        private static readonly string[] guns =
        [
            "weapon_deagle",
            "weapon_taser",
            "weapon_knife",
            "weapon_bayonet",
            "weapon_revolver",
            "weapon_glock",
            "weapon_usp_silencer",
            "weapon_cz75a",
            "weapon_fiveseven",
            "weapon_p250",
            "weapon_tec9",
            "weapon_elite",
            "weapon_hkp2000"
        ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Komandos", 
            "Potrójne obrażenia z pistoletów i noża", 
            "#c57f30");
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

            var activeWeapon = attackerPawn.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon != null && guns.Contains(activeWeapon.DesignerName))
                info.Damage *= 3.0f;

            return HookResult.Continue;
        }
    }
}