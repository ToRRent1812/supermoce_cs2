using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Illusionist : ISkill
    {
        private const Skills skillName = Skills.Illusionist;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly ConcurrentDictionary<int, Timer> ActiveTimers = [];
        private static readonly object setLock = new();
        private static int cd = 30;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Android", "Możesz stworzyć hologram idący przed siebie", "#eeff00");
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(4, 11)) ?? 4) * 5;
            ClearAllReplicas();
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        private static void ClearAllReplicas()
        {
            foreach (var timer in ActiveTimers.Values) timer?.Kill();
            ActiveTimers.Clear();

            var entities = Utilities.FindAllEntitiesByDesignerName<CDynamicProp>("prop_dynamic_override");
            foreach (var entity in entities)
                if (entity != null && entity.IsValid && entity.Entity != null && !string.IsNullOrEmpty(entity.Entity.Name) && (entity.Entity.Name?.StartsWith("Illusionist_") ?? false))
                    entity.AcceptInput("Kill");
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName && SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                    UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0
                ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>"
                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            player.PrintToCenterHtml(skillLine + remainingLine);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo) && skillInfo.CanUse)
            {
                skillInfo.CanUse = false;
                skillInfo.Cooldown = DateTime.Now;
                CreateReplica(player);
            }
        }

        private static void CreateReplica(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            if (playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null) return;

            var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (replica == null || !replica.IsValid) return;

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

            Instance?.AddTickTimer(10, () => {
                var moveTimer = Instance.AddTickTimer(1, () =>
                {
                    if (replica == null || !replica.IsValid)
                    {
                        if (ActiveTimers.TryRemove(replicaIndex, out var timer)) timer?.Kill();
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
                }
            });
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return;

            if (string.IsNullOrEmpty(param.Entity.Name)) return;
            if (!param.Entity.Name.StartsWith("Illusionist_")) return;

            var replica = param.As<CDynamicProp>();
            if (replica == null || !replica.IsValid) return;

            replica.EmitSound("GlassBottle.BulletImpact", volume: 1f);
            if (ActiveTimers.TryRemove((int)replica.Index, out var timer)) timer?.Kill();
            replica.AcceptInput("Kill");

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            if (attackerPawn.DesignerName != "player") return;

            var attackerTeam = attackerPawn.TeamNum;
            var replicaTeam = replica.Globalname.EndsWith("CT") ? 3 : 2;
            SkillUtils.TakeHealth(attackerPawn, attackerTeam != replicaTeam ? 20 : 10);
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
        }
    }
}