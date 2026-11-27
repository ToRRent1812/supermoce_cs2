using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Noclip : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Noclip;
        private static readonly ConcurrentDictionary<int, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Noclip",
                "Możesz włączyć sobie noclip na 2 sek.",
                "#44ebd4",
                minCooldown: 15,
                maxCooldown: 60,
                cooldownStep: 5);
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
                if (playerInfo?.Skill == skillName && SkillPlayerInfo.TryGetValue(player.Slot, out var skillInfo))
                    UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);

            SkillPlayerInfo.TryAdd(player.Slot, new PlayerSkillInfo
            {
                Slot = player.Slot,
                IsFlying = false,
                LastPosition = null,
                LastUseTime = -1000000f
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
            SkillPlayerInfo.TryRemove(player.Slot, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            float flying = 0;

            if (skillInfo != null)
            {
                if (ActiveSkillFramework.TryGetSkillState(skillName, player, out var state))
                {
                    if(state == null) return;
                    int elapsed = (int)(Server.CurrentTime - state.LastUseTime);
                    cooldown = Math.Max(state.CooldownSeconds - elapsed, 0);
                }

                float flyingTime = (skillInfo.LastUseTime + 2f - Server.CurrentTime) * 1000f;
                flying = Math.Max(flyingTime, 0);
            }

            string status = flying > 0
                ? $"Aktywne przez {Math.Round(flying / 1000, 1)} sek."
                : cooldown > 0
                    ? $"Poczekaj {cooldown} sek."
                    : "Wciśnij INSPEKT by użyć";

            SkillHudPanel.ShowStatus(player, skillName, status);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || player.IsValid == false || player.PawnIsAlive == false) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!SkillPlayerInfo.TryGetValue(player.Slot, out var skillInfo))
                return;

            if (skillInfo.IsFlying)
            {
                skillInfo.IsFlying = false;
                playerPawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;

                Instance?.AddTimer(4f, () =>
                {
                    if (playerPawn == null || !playerPawn.IsValid || !player.PawnIsAlive || skillInfo.IsFlying) return;
                    if (skillInfo.LastPosition == null || playerPawn.AbsOrigin == null) return;
                    var diff = Math.Abs(playerPawn.AbsOrigin.Z - skillInfo.LastPosition.Z);
                    if (diff > 3000 && playerPawn.AbsOrigin.Z < skillInfo.LastPosition.Z)
                        playerPawn.Teleport(skillInfo.LastPosition, null, new Vector(0, 0, 0));
                });
                return;
            }

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config == null) return;
            if (!ActiveSkillFramework.CanUseSkill(skillName, player)) return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            skillInfo.IsFlying = true;
            skillInfo.LastUseTime = Server.CurrentTime;
            skillInfo.LastPosition = playerPawn.AbsOrigin == null ? null : new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z);

            playerPawn.ActualMoveType = MoveType_t.MOVETYPE_NOCLIP;
            Instance?.AddTimer(2f, () =>
            {
                if (playerPawn == null || !playerPawn.IsValid || !skillInfo.IsFlying) return;
                skillInfo.IsFlying = false;
                playerPawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
            });

            Instance?.AddTimer(6f, () =>
            {
                if (playerPawn == null || !playerPawn.IsValid || !player.PawnIsAlive || skillInfo.IsFlying) return;
                if (skillInfo.LastPosition == null || playerPawn.AbsOrigin == null) return;
                var diff = Math.Abs(playerPawn.AbsOrigin.Z - skillInfo.LastPosition.Z);
                if (diff > 3000 && playerPawn.AbsOrigin.Z < skillInfo.LastPosition.Z)
                    playerPawn.Teleport(skillInfo.LastPosition, null, new Vector(0, 0, 0));
            });
        }

        public class PlayerSkillInfo
        {
            public int Slot { get; set; }
            public bool IsFlying { get; set; }
            public float LastUseTime { get; set; }
            public Vector? LastPosition { get; set; }
        }
    }
}
