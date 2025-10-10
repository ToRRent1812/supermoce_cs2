using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Commando : ISkill
    {
        private const Skills skillName = Skills.Commando;

        private static readonly string[] guns =
        [
            "weapon_deagle",
            "weapon_taser",
            "weapon_knife",
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
            SkillUtils.RegisterSkill(skillName, "Komandos", "Potrójne obrażenia z pistoletów i noża", "#c57f30");
        }
        
        public static void OnTakeDamage(DynamicHook h)
        {
            var param = h.GetParam<CEntityInstance>(0);
            var param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param?.Entity == null || param2?.Attacker?.Value == null)
                return;

            var attackerPawn = new CCSPlayerPawn(param2.Attacker.Value.Handle);
            var victimPawn = new CCSPlayerPawn(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return;

            var attackerController = attackerPawn.Controller?.Value?.As<CCSPlayerController>();
            if (attackerController == null || victimPawn.Controller?.Value == null)
                return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerController.SteamID);
            if (playerInfo?.Skill != skillName || !attackerController.PawnIsAlive)
                return;

            var activeWeapon = attackerPawn.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon != null && guns.Contains(activeWeapon.DesignerName))
                param2.Damage *= 3.0f;
        }
    }
}