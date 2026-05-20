using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using RayTraceAPI;
using TraceOptions = RayTraceAPI.TraceOptions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public static class RayTrace
    {
        private static PluginCapability<CRayTraceInterface> RayTraceInterface { get; } = new("raytrace:craytraceinterface");

        public static CustomTraceResult? TraceShape(CCSPlayerController player, Vector startPos, Vector endPos, ulong? mask = null, ulong? contents = null)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return null;

            var rayTrace = RayTraceInterface.Get();
            if (rayTrace == null)
                return null;

            mask ??= playerPawn.Collision.CollisionAttribute.InteractsWith | (ulong)InteractionLayers.Hitboxes;
            mask &= ~(ulong)InteractionLayers.PlayerClip;
            contents ??= 0;

            TraceOptions options = new()
            {
                InteractsWith = (ulong)mask,
                InteractsExclude = (ulong)contents,
                DrawBeam = 0,
            };

            rayTrace.TraceEndShape(startPos, endPos, playerPawn, options, out TraceResult result);
            var customResult = new CustomTraceResult(result, startPos, (ulong)mask, (ulong)contents);

            return customResult;
        }

        public static CustomTraceResult? EyeTrace(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return null;

            float maxDistance = 4096f;

            Vector startPos = new(playerPawn.AbsOrigin!.X, playerPawn.AbsOrigin!.Y, playerPawn.AbsOrigin!.Z + playerPawn.ViewOffset.Z);
            Vector endPos = startPos + SkillUtils.GetForwardVector(playerPawn.EyeAngles) * maxDistance;

            return TraceShape(player, startPos, endPos);
        }

        public static CustomTraceResult? TraceHullShape(Vector startPos, Vector endPos, CCSPlayerController player, Vector? mins = null, Vector? maxs = null, ulong? mask = null, ulong? contents = null, QAngle? angle = null)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null)
                return null;

            var rayTrace = RayTraceInterface.Get();
            if (rayTrace == null)
                return null;

            mask ??= playerPawn.Collision.CollisionAttribute.InteractsWith;
            contents ??= 0; // playerPawn.Collision.CollisionGroup;


            TraceOptions options = new()
            {
                InteractsWith = (ulong)mask,
                InteractsExclude = (ulong)contents,
            };

            mins ??= playerPawn.Collision.Mins;
            maxs ??= playerPawn.Collision.Maxs;

            rayTrace.TraceHullShape(startPos, endPos, mins, maxs, playerPawn, options, out TraceResult result);

            return new CustomTraceResult(result, startPos, (ulong)mask, (ulong)contents);
        }

        public static bool HitPlayer(this CustomTraceResult result, out CCSPlayerController? player)
        {
            if (result.HitEntity == nint.Zero)
            {
                player = null;
                return false;
            }

            try
            {
                CEntityInstance entityInstance = new(result.HitEntity);
                if (string.IsNullOrEmpty(entityInstance.DesignerName) || !entityInstance.DesignerName.Equals("player"))
                {
                    player = null;
                    return false;
                }

                var playerPawn = entityInstance.As<CCSPlayerPawn>();
                if (playerPawn?.OriginalController?.Value != null)
                {
                    player = playerPawn.OriginalController.Value;
                    return true;
                }
            }
            catch { }

            player = null;
            return false;
        }
    }
}
