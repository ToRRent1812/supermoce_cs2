using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Giant : ISkill
    {
        private const Skills skillName = Skills.Giant;
        private static readonly ConcurrentDictionary<ulong, int> giants = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Olbrzym", "Wybierasz wroga, którego powiększysz", "#e621d6");
        }

        public static void NewRound()
        {
            giants.Clear();
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid) continue;
                SkillUtils.CloseMenu(player);
                SkillUtils.ChangePlayerScale(player, 1f);
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn == null || !playerPawn.IsValid) continue;
                playerPawn.VelocityModifier = 1f;
                playerPawn.ActualGravityScale = 1f;
            }
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!giants.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier == 0) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 0.7f;
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
            
            var EnemyPawn = enemy.PlayerPawn.Value;
            if (EnemyPawn != null && EnemyPawn.IsValid)
            {
                SkillUtils.ChangePlayerScale(enemy, 2f);
                EnemyPawn.VelocityModifier = 0.7f;
                EnemyPawn.ActualGravityScale = 0.9f;
                SkillUtils.PrintToChat(enemy, $"Wróg Cię powiększył.");
                giants.TryAdd(enemy.SteamID, 0);
            }
            playerInfo.SkillChance = 1;
            
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
            SkillUtils.CloseMenu(player);
            SkillUtils.ChangePlayerScale(player, 1f);
            giants.TryRemove(player.SteamID, out _);
        }
    }
}