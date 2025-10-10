using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Noclip : ISkill
    {
        private const Skills skillName = Skills.Noclip;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();
        private static int cd = 20;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Bez barier", "Możesz włączyć sobie noclip na 2 sek.", "#44ebd4");
        }

        public static void NewRound()
        {
            cd = ((Instance?.Random.Next(3, 11)) ?? 3) * 5;
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                        UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                IsFlying = false,
                Cooldown = DateTime.MinValue,
                LastPosition = null,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            float cooldown = 0;
            float flying = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                float flyingTime = (int)(skillInfo.Cooldown.AddSeconds(2f) - DateTime.Now).TotalMilliseconds;
                flying = Math.Max(flyingTime, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0
                                    ? (
                                        flying != 0
                                        ? $"<font class='fontSize-m' color='#FFFFFF'>Aktywne przez <font color='#00FF00'>{Math.Round(flying / 1000, 1)}</font> sek.</font>"
                                        : $"<font class='fontSize-m' color='#FFFFFF'>Poczekaj <font color='#FF0000'>{cooldown}</font> sek.</font>"
                                    ) : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

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
                    skillInfo.IsFlying = true;
                    skillInfo.Cooldown = DateTime.Now;
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
                        skillInfo.IsFlying = false;
                        var diff = Math.Abs(playerPawn.AbsOrigin.Z - skillInfo.LastPosition.Z);
                        if (diff > 3000 && playerPawn.AbsOrigin.Z < skillInfo.LastPosition.Z)
                            playerPawn.Teleport(skillInfo.LastPosition, null, new Vector(0, 0, 0));
                    });
                }
                else if (skillInfo.IsFlying)
                {
                    skillInfo.IsFlying = false;
                    playerPawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;

                    Instance?.AddTimer(4f, () =>
                    {
                        if (playerPawn == null || !playerPawn.IsValid || !player.PawnIsAlive || skillInfo.IsFlying) return;
                        if (skillInfo.LastPosition == null || playerPawn.AbsOrigin == null) return;
                        skillInfo.IsFlying = false;
                        var diff = Math.Abs(playerPawn.AbsOrigin.Z - skillInfo.LastPosition.Z);
                        if (diff > 3000 && playerPawn.AbsOrigin.Z < skillInfo.LastPosition.Z)
                            playerPawn.Teleport(skillInfo.LastPosition, null, new Vector(0, 0, 0));
                    });
                }
            }
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public bool IsFlying { get; set; }
            public DateTime Cooldown { get; set; }
            public Vector? LastPosition { get; set; }
        }
    }
}