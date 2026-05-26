using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class BrokenKnee : ISkill
    {
        private const Skills skillName = Skills.BrokenKnee;

        private static readonly ConcurrentDictionary<ulong, int> affectedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Strzał w kolano", "Wybierasz gracza, który będzie chodzić wolniej", "#e68a21");
        }

        public static void NewRound()
        {
            affectedPlayers.Clear();
            foreach (var player in Utilities.GetPlayers())
            {
                SkillUtils.CloseMenu(player);
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null && player.PawnIsAlive)
                    playerPawn.VelocityModifier = 1f;
            }

        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if(!affectedPlayers.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier == 0) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 0.75f;
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

            if (enemy.PlayerPawn?.Value != null && enemy.PlayerPawn.Value.IsValid)
                enemy.PlayerPawn.Value.VelocityModifier = 0.75f;
            
            playerInfo.SkillChance = 1;
            affectedPlayers.TryAdd(enemy.SteamID, 0);
            SkillUtils.PrintToChat(enemy, $"Wróg spowodował, że poruszasz się wolniej.");
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
        }
    }
}
