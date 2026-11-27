using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;

namespace Supermoce
{
    public class Anomaly : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Anomaly;
        private static readonly int maxSize = 4;
        private static readonly ConcurrentDictionary<int, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Lag Switch",
                "Cofasz swój ruch o 4 sekundy",
                "#a86eff",
                minCooldown: 15,
                maxCooldown: 30,
                cooldownStep: 3);
        }

        public static void NewRound()
        {
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;

            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill != skillName) continue;

                if (!SkillPlayerInfo.TryGetValue(player.Slot, out var skillInfo))
                    continue;

                if (Server.TickCount % 64 != 0) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;
                if (skillInfo.LastRotations == null || skillInfo.LastPositions == null) continue;

                skillInfo.LastPositions.Enqueue(new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z));
                skillInfo.LastRotations.Enqueue(new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z));
                if (skillInfo.LastRotations.Count > maxSize)
                {
                    skillInfo.LastPositions.TryDequeue(out _);
                    skillInfo.LastRotations.TryDequeue(out _);
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.Slot, new PlayerSkillInfo
            {
                Slot = player.Slot,
                LastPositions = [],
                LastRotations = [], 
            });

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
            {
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);

                if (ActiveSkillFramework.TryGetSkillState(skillName, player, out var state) && state != null)
                    state.LastUseTime = Server.CurrentTime - (state.CooldownSeconds - 10);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.Slot, out _);
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;
            if (!SkillPlayerInfo.TryGetValue(player.Slot, out var skillInfo)) return;
            if (skillInfo.LastRotations == null || skillInfo.LastRotations.Count == 0 || skillInfo.LastPositions == null || skillInfo.LastPositions.Count == 0)
                return;

            Vector? lastPosition = skillInfo.LastPositions.FirstOrDefault();
            QAngle? lastRotation = skillInfo.LastRotations.FirstOrDefault();
            if (lastPosition == null || lastRotation == null) return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            playerPawn.Teleport(lastPosition, lastRotation, null);
        }

        public class PlayerSkillInfo
        {
            public int Slot { get; set; }
            public ConcurrentQueue<Vector>? LastPositions { get; set; }
            public ConcurrentQueue<QAngle>? LastRotations { get; set; }
        }
    }
}