using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Noclip : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Noclip;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
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
                cooldownStep: 5,
                useCustomHud: true);
        }

        public static void NewRound()
        {
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName && SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                    UpdateHUD(player, skillInfo);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);

            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                IsFlying = false,
                LastPosition = null,
                LastUseTime = DateTime.MinValue
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
            SkillPlayerInfo.TryRemove(player.SteamID, out _);
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
                    int elapsed = (int)(DateTime.Now - state.LastUseTime).TotalSeconds;
                    cooldown = Math.Max(state.CooldownSeconds - elapsed, 0);
                }

                float flyingTime = (int)(skillInfo.LastUseTime.AddSeconds(2f) - DateTime.Now).TotalMilliseconds;
                flying = Math.Max(flyingTime, 0);
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font>";
            string remainingLine = cooldown != 0
                                    ? (
                                        flying != 0
                                            ? $"<br><font class='fontSize-m' color='#FFFFFF'>Aktywne przez <font color='#00FF00'>{Math.Round(flying / 1000, 1)}</font> sek.</font>"
                                            : $"<br><font class='fontSize-m' color='#ffe3d6'>Poczekaj <font color='#ffa600'>{cooldown}</font> sek.</font>"
                                    ) : $"<br><font class='fontSize-s' class='fontWeight-Bold' color='#deff24'>Wciśnij INSPEKT by użyć</font>";

            var hudContent = skillLine + remainingLine;
            ActiveSkillFramework.PrintCachedHud(player, hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || player.IsValid == false || player.PawnIsAlive == false) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
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
            skillInfo.LastUseTime = DateTime.Now;
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
            public ulong SteamID { get; set; }
            public bool IsFlying { get; set; }
            public DateTime LastUseTime { get; set; }
            public Vector? LastPosition { get; set; }
        }
    }
}
