using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using RayTraceAPI;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class Shade : ISkill
    {
        private const Skills skillName = Skills.Shade;
        private static readonly ConcurrentDictionary<CCSPlayerController, float> noSpace = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Cień", "25% szans na pojawienie się za plecami trafionego wroga", "#4d4d4d");
        }

        public static void NewRound()
        {
            noSpace.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false) return;

            var victimInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && Instance?.Random.Next(1,5) == 1)
                TeleportAttackerBehindVictim(attacker!, victim!);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            noSpace.TryRemove(player, out _);
        }

        public static void OnTick()
        {
            foreach (var item in noSpace)
                if (item.Value >= Server.TickCount)
                    UpdateHUD(item.Key);
        }

        private static void UpdateHUD(CCSPlayerController player)
        {
            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = $"<font class='fontSize-s' color='#FF0000'>Brak miejsca za wrogiem</font>";
            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        private unsafe static bool CheckTeleport(CCSPlayerController player, Vector startPos, Vector endPos)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return false;
            var rayTrace = RayTraceInterface.Get();
            if (rayTrace == null) return false;

            TraceOptions options = new();
            options.InteractsWith = (ulong)InteractionLayers.MASK_SHOT_PHYSICS;
            options.InteractsExclude = 0;
            options.DrawBeam = 0;

            Vector mins = new(-16f, -16f, 0f);
            Vector maxs = new(16f, 16f, 72f);

            if (!rayTrace.TraceHullShape(startPos, endPos, mins, maxs, null, options, out TraceResult traceResult))
                return true;

            // return true if we didn't hit world geometry (i.e. either no hit or hit an entity)
            return !traceResult.DidHit || traceResult.HitEntity != 0;
        }

        private static void TeleportAttackerBehindVictim(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var victimPawn = victim.PlayerPawn.Value;
            var attackerPawn = attacker.PlayerPawn.Value;

            if (victimPawn == null || attackerPawn == null || victimPawn.AbsOrigin == null || victimPawn.AbsRotation == null) return;

            QAngle victimAngles = victimPawn.AbsRotation;
            Vector victimEyePos = new(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z + victimPawn.ViewOffset.Z);
            int[] angles = [0, 90, -90];

            bool teleported = false;
            foreach (int extraAngle in angles)
            {
                QAngle newAngle = new(victimAngles.X, victimAngles.Y + extraAngle, victimAngles.Z);
                Vector behindPosition = victimEyePos - SkillUtils.GetForwardVector(newAngle) * 100f;
                if (!CheckTeleport(victim, victimEyePos, behindPosition)) continue;
                attackerPawn.Teleport(behindPosition, newAngle, new(0, 0, 0));
                teleported = true;
                break;
            }
            if (!teleported)
                noSpace.AddOrUpdate(attacker, Server.TickCount + 128, (k, v) => Server.TickCount + 128);
        }
    }
}
