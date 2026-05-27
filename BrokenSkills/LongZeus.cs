using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class LongZeus : ISkill
    {
        private const Skills skillName = Skills.LongZeus;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Railgun", "Zeus ma zasięg snajperki", "#6effc7");
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if (Instance == null || !Instance.IsPlayerValid(player)) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player!.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.WeaponServices == null) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon?.Value;
            if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.DesignerName != "weapon_taser") return;

            var result = RayTrace.EyeTrace(player);
            if (result == null || !result.HasValue) return;

            if (!result.Value.HitPlayer(out CCSPlayerController? target) || target == null) return;

            if (target.Handle == player.Handle) return;
            SkillUtils.TakeHealth(target.PlayerPawn.Value, 999);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Zeus);
        }
    }
}