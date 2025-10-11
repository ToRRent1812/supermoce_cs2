using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Zeus : ISkill
    {
        private const Skills skillName = Skills.Zeus;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Super Zeus", "Zeus natychmiastowo się odnawia", "#fbff00", 2);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Zeus);
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);

            if (playerInfo?.Skill == skillName)
            {
                var pawn = player!.PlayerPawn!.Value!;
                if (pawn.WeaponServices == null || pawn.WeaponServices.ActiveWeapon == null || !pawn.WeaponServices.ActiveWeapon.IsValid) return;
                if (pawn.WeaponServices.ActiveWeapon.Value == null || !pawn.WeaponServices.ActiveWeapon.Value.IsValid) return;

                var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
                if (activeWeapon.DesignerName != "weapon_taser") return;
                var taser = activeWeapon.As<CWeaponTaser>();
                Instance?.AddTimer(.2f, () =>
                {
                    if (taser.IsValid)
                    {
                        taser.LastAttackTick = 0;
                        taser.FireTime = 0;
                    }
                });
            }
        }
    }
}