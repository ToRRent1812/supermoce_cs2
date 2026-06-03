using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class JumpBan : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.JumpBan;
        private static readonly ConcurrentDictionary<ulong, int> bannedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Beznogi", 
            "Wybierasz gracza, który nie będzie mógł skakać", 
            "#b01e5d");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();

            foreach (var kv in bannedPlayers)
            {
                var pl = Utilities.GetPlayers().FirstOrDefault(p => p?.SteamID == kv.Key);
                if (pl != null && pl.IsValid && pl.PlayerPawn?.Value != null && pl.PlayerPawn.Value.IsValid)
                    pl.PlayerPawn.Value.ActualGravityScale = 1f;
            }

            bannedPlayers.Clear();
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            if (!bannedPlayers.ContainsKey(player.SteamID)) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;
            pawn.AbsVelocity.Z = -200;
            pawn.ActualGravityScale = 4f;
        }

        public static void OnTick()
        {
            if (bannedPlayers.IsEmpty) return;
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!bannedPlayers.ContainsKey(player.SteamID)) continue;
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid) continue;
                if (pawn.AbsVelocity.Z > 0)
                    pawn.AbsVelocity.Z = 0;
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy))
                return;

            bannedPlayers[enemy!.SteamID] = 1;
            var enemyPawn = enemy.PlayerPawn?.Value;
            if (enemyPawn != null && enemyPawn.IsValid)
                enemyPawn.ActualGravityScale = 4f;

            SkillUtils.PrintToChat(enemy, $"Wróg zabronił Ci skakania.", true);
            if (playerInfo != null) playerInfo.SkillChance = 1;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            SkillUtils.DestroyTargetingSkill(player, p => bannedPlayers.TryRemove(p.SteamID, out _));
        }
    }
}