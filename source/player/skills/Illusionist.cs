using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Illusionist : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Illusionist;
        private static readonly ConcurrentDictionary<ulong, uint> ActiveReplicas = [];
        private static readonly ConcurrentDictionary<int, Timer> ActiveTimers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Android",
                "Możesz stworzyć hologram idący przed siebie",
                "#eeff00",
                minCooldown: 20,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
            foreach (var timer in ActiveTimers.Values) timer?.Kill();
            ActiveTimers.Clear();

            foreach (var replicaIndex in ActiveReplicas.Values)
                SkillUtils.SafeKillEntity<CDynamicProp>(replicaIndex);

            ActiveReplicas.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            ActiveSkillFramework.OnSkillDisabled(skillName, player);

            if (ActiveReplicas.TryRemove(player.SteamID, out var replicaIndex))
            {
                SkillUtils.SafeKillEntity<CDynamicProp>(replicaIndex);
                if (ActiveTimers.TryRemove((int)replicaIndex, out var timer))
                    timer?.Kill();
            }
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null) return;

            if (ActiveReplicas.TryRemove(player.SteamID, out var replicaIndex))
            {
                SkillUtils.SafeKillEntity<CDynamicProp>(replicaIndex);
                if (ActiveTimers.TryRemove((int)replicaIndex, out var timer))
                    timer?.Kill();
            }

            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            if (!ActiveSkillFramework.CanUseSkill(skillName, player)) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || playerPawn.CBodyComponent == null || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
                return;

            if (!((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
            {
                SkillUtils.PrintToChat(player, "Musisz stać na ziemi!", true);
                return;
            }

    if (!CreateReplica(player))
    {
        SkillUtils.PrintToChat(player, $"Nie udało się stworzyć hologramu!", true);
        return;
    }

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
        }

        private static bool CreateReplica(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return false;
            if (playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return false;

            if (ActiveReplicas.TryRemove(player.SteamID, out var existingReplicaIndex))
            {
                SkillUtils.SafeKillEntity<CDynamicProp>(existingReplicaIndex);
                if (ActiveTimers.TryRemove((int)existingReplicaIndex, out var oldTimer))
                    oldTimer?.Kill();
            }

            var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (replica == null || !replica.IsValid) return false;

            replica.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

            replica.SetModel(playerPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            replica.Entity!.Name = replica.Globalname = $"Illusionist_{Server.TickCount}_{(player.Team == CsTeam.CounterTerrorist ? "CT" : "TT")}";

            replica.UseAnimGraph = false;
            replica.DispatchSpawn();

            float distance = 40;
            Vector startPos = playerPawn.AbsOrigin + SkillUtils.GetForwardVector(playerPawn.AbsRotation) * distance;
            QAngle angle = new(0, playerPawn.EyeAngles.Y, 0);
            replica.Teleport(startPos, angle, new Vector(0, 0, -100));

            bool ducking = ((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING);
            string animName = ducking ? "crouch_new_knife_n" : "run_new_knife_n";
            replica.AcceptInput("SetAnimation", value: animName);
            replica.AcceptInput("SetPlaybackRate", value: "1.0");

            float speed = ducking ? 1.25f : 3.5f;
            Vector forwardVec = SkillUtils.GetForwardVector(angle);
            int replicaIndex = (int)replica.Index;

            Instance?.AddTickTimer(10, () =>
            {
                var moveTimer = Instance.AddTickTimer(1, () =>
                {
                    if (replica == null || !replica.IsValid)
                    {
                        if (ActiveTimers.TryRemove(replicaIndex, out var timer)) timer?.Kill();
                        ActiveReplicas.TryRemove(player.SteamID, out _);
                        return;
                    }

                    if (ducking && Server.TickCount % 50 == 0)
                    {
                        replica.AcceptInput("SetAnimation", value: animName);
                        replica.AcceptInput("SetPlaybackRate", value: "1.0");
                    }

                    Vector currentPos = replica.AbsOrigin!;
                    Vector nexPos = new(
                        currentPos.X + (forwardVec.X * speed),
                        currentPos.Y + (forwardVec.Y * speed),
                        currentPos.Z
                    );
                    replica.Teleport(nexPos, null, null);
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

                ActiveTimers.TryAdd(replicaIndex, moveTimer);
            });

            float duration = ducking ? 12 : 5;
            Instance?.AddTimer(duration, () =>
            {
                if (replica != null && replica.IsValid)
                {
                    replica.AcceptInput("Kill");
                    if (ActiveTimers.TryRemove(replicaIndex, out var timer)) timer?.Kill();
                    ActiveReplicas.TryRemove(player.SteamID, out _);
                }
            });

            ActiveReplicas.TryAdd(player.SteamID, (uint)replicaIndex);
            return true;
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            if (string.IsNullOrEmpty(entity.Entity?.Name)) return HookResult.Continue;
            if (!entity.Entity.Name.StartsWith("Illusionist_")) return HookResult.Continue;

            var replica = entity.As<CPhysicsPropMultiplayer>();
            if (replica == null || !replica.IsValid) return HookResult.Continue;
            replica.EmitSound("GlassBottle.BulletImpact", volume: 1f);
            if (ActiveTimers.TryRemove((int)replica.Index, out var timer)) timer?.Kill();
            replica.AcceptInput("Kill");

            ulong? staleKey = null;
            foreach (var kvp in ActiveReplicas)
            {
                if (kvp.Value == replica.Index)
                {
                    staleKey = kvp.Key;
                    break;
                }
            }
            if (staleKey != null) ActiveReplicas.TryRemove(staleKey.Value, out _);

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            var attackerTeam = attackerPawn.TeamNum;
            var replicaTeam = replica.Globalname.EndsWith("CT") ? 3 : 2;
            SkillUtils.TakeHealth(attackerPawn, attackerTeam != replicaTeam ? 20 : 10);

            return HookResult.Continue;
        }
    }
}
