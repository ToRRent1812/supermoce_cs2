using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using System.Numerics;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class Shade : ISkill
    {
        private const Skills skillName = Skills.Shade;
        private static readonly float teleportDistance = Config.GetValue<float>(skillName, "teleportDistance");
        private static readonly Dictionary<CCSPlayerController, float> noSpace = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            noSpace.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim)) return;

            var victimInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && Instance.Random.Next(1,2) == 1)
                TeleportAttackerBehindVictim(attacker!, victim!);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            noSpace.Remove(player);
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
            string remainingLine = $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("shade_nospace")}</font>";
            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        private unsafe static bool CheckTeleport(CCSPlayerController player, Vector startPos, Vector endPos)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return false;
            Ray ray = new(new Vector3(-16, -16, -0), new Vector3(16, 16, 72));
            CTraceFilter filter = new(pawn.Index, pawn.Index)
            {
                m_nObjectSetMask = 0xf,
                m_nCollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT,
                m_nInteractsWith = pawn.GetInteractsWith(),
                m_nInteractsExclude = 0,
                m_nBits = 11,
                m_bIterateEntities = true,
                m_bHitTriggers = false,
                m_nInteractsAs = 0x40000
            };

            filter.m_nHierarchyIds[0] = pawn.GetHierarchyId();
            filter.m_nHierarchyIds[1] = 0;
            CGameTrace trace = TraceRay.TraceHull(startPos, endPos, filter, ray);
            return !trace.HitWorld(out _);
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
                Vector behindPosition = victimEyePos - SkillUtils.GetForwardVector(newAngle) * teleportDistance;
                if (!CheckTeleport(victim, victimEyePos, behindPosition)) continue;
                attackerPawn.Teleport(behindPosition, newAngle, new(0, 0, 0));
                teleported = true;
                break;
            }
            if (!teleported)
                noSpace[attacker] = Server.TickCount + (64 * 2);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#4d4d4d", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float teleportDistance = 100f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float TeleportDistance { get; set; } = teleportDistance;
        }
    }
}
