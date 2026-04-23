using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class JumpBan : ISkill
    {
        private const Skills skillName = Skills.JumpBan;
        // Key by SteamID for stability across pawn object changes
        public static readonly ConcurrentDictionary<ulong, int> bannedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Beznogi", "Wybierasz gracza, który nie będzie mógł skakać", "#b01e5d");
        }

        public static void NewRound()
        {
            // Revert gravity for any banned players before clearing the list
            foreach (var kv in bannedPlayers)
            {
                var pl = Utilities.GetPlayers().FirstOrDefault(p => p?.SteamID == kv.Key);
                if (pl != null && pl.IsValid && pl.PlayerPawn?.Value != null && pl.PlayerPawn.Value.IsValid)
                {
                    pl.PlayerPawn.Value.ActualGravityScale = 1f;
                }
            }

            bannedPlayers.Clear();
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            if (!bannedPlayers.ContainsKey(player.SteamID)) return;

            // Immediately cancel the jump by forcing downward velocity and ensure high gravity
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;
            pawn.AbsVelocity.Z = -200;
            pawn.ActualGravityScale = 5f;
        }

        public static void OnTick()
        {
            // Ensure banned players keep high gravity and cannot gain upward velocity
            foreach (var kv in bannedPlayers)
            {
                var steamID = kv.Key;
                var pl = Utilities.GetPlayers().FirstOrDefault(p => p?.SteamID == steamID);
                if (pl == null || !pl.IsValid || pl.PlayerPawn?.Value == null || !pl.PlayerPawn.Value.IsValid) continue;
                var pawn = pl.PlayerPawn.Value;
                pawn.ActualGravityScale = 5f;
                if (pawn.AbsVelocity.Z > 0)
                    pawn.AbsVelocity.Z = 0;
            }

            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if (!SkillUtils.HasMenu(player)) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();

                ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => (e.PlayerName, e.Index.ToString()))];
                SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                player.PrintToChat($" {ChatColors.Red}Nie posiadasz już tej supermocy");
                return;
            }

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Team != player.Team && p.Index.ToString() == enemyId);

            if (enemy == null || !enemy.IsValid || enemy.PlayerPawn.Value == null || !enemy.PlayerPawn.Value.IsValid)
            {
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
                return;
            }

            // mark steamID as banned (value unused) and apply immediate high gravity
            bannedPlayers[enemy.SteamID] = 1;
            if (enemy.PlayerPawn?.Value != null && enemy.PlayerPawn.Value.IsValid)
            {
                enemy.PlayerPawn.Value.ActualGravityScale = 5f;
            }
            playerInfo.SkillChance = 1;
            player.PrintToChat($" {ChatColors.Green}{enemy.PlayerName} nie może teraz skakać.");
            enemy.PrintToChat($" {ChatColors.Red}Wróg odciął ci nogi.");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => (e.PlayerName, e.Index.ToString()))];
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            // attempt to remove any ban matching this player's SteamID (no target-owner mapping available)
            bannedPlayers.TryRemove(player.SteamID, out _);
            SkillUtils.CloseMenu(player);
        }
    }
}