using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using RayTraceAPI;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class LongZeus : ISkill
    {
        private const Skills skillName = Skills.LongZeus;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Railgun", "Zeus ma nielimitowany zasięg", "#6effc7");
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player!.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.WeaponServices == null) return;

            if (pawn.WeaponServices.ActiveWeapon == null || !pawn.WeaponServices.ActiveWeapon.IsValid) return;
            if (pawn.WeaponServices.ActiveWeapon.Value == null || !pawn.WeaponServices.ActiveWeapon.Value.IsValid) return;

            var activeWeapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (activeWeapon.DesignerName != "weapon_taser") return;

            Vector eyePos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);
            Vector endPos = eyePos + SkillUtils.GetForwardVector(pawn.EyeAngles) * 4096f;
            var rayTrace = RayTraceInterface.Get();
            if (rayTrace == null) return;

            TraceOptions options = new();
            options.InteractsWith = (ulong)InteractionLayers.MASK_SHOT_PHYSICS;
            options.InteractsExclude = 0;
            options.DrawBeam = 0;

            if (!rayTrace.TraceEndShape(eyePos, endPos, null, options, out TraceResult traceResult))
                return;

            var target = SkillUtils.GetPlayerFromTraceResult(traceResult);
            if (target == null) return;

            if (target.Handle == player.Handle) return;
            SkillUtils.TakeHealth(target.PlayerPawn.Value, 9999);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Zeus);
        }
    }
}