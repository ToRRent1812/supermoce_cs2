using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Anomaly : ISkill
    {
        private const Skills skillName = Skills.Anomaly;
        private static readonly int maxSize = 3; //Seconds in back
        private static readonly float tickRate = 64;
        private static int cd = 15;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Lag Switch", "Cofasz swój ruch o 3 sekundy", "#a86eff");
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(3, 6)) ?? 3) * 5;
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                    {
                        UpdateHUD(player, skillInfo);
                        if (Server.TickCount % tickRate != 0) return;
                        var pawn = player.PlayerPawn.Value;
                        if (pawn != null && pawn.IsValid && pawn.AbsOrigin != null)
                        {
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
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                LastPositions = [],
                LastRotations = [], 
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

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.IsValid || !player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;
                    if (skillInfo.LastRotations == null || skillInfo.LastRotations.Count == 0 || skillInfo.LastPositions == null || skillInfo.LastPositions.Count == 0) return;
                    Vector? lastPosition = skillInfo.LastPositions.FirstOrDefault();
                    QAngle? lastRotation = skillInfo.LastRotations.FirstOrDefault();
                    if (lastPosition != null && lastRotation != null)
                        playerPawn.Teleport(lastPosition, lastRotation, null);
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
            public ConcurrentQueue<Vector>? LastPositions { get; set; }
            public ConcurrentQueue<QAngle>? LastRotations { get; set; }
        }
    }
}