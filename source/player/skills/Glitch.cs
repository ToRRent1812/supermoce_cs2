using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Glitch : ISkill
    {
        private const Skills skillName = Skills.Glitch;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                "Anty-Radar",
                "Przeciwnicy grają rundę bez radaru",
                "#f542ef");
        }

        public static void NewRound()
        {
            var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
            if (players.Length <= 0) return;
            foreach (var player in players)
                player.ReplicateConVar("sv_disable_radar", "0");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length <= 0) return;
            foreach (var enemy in enemies)
                enemy.ReplicateConVar("sv_disable_radar", "1");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            bool stillHasGlitch = SkillUtils.CachedPlayers
                .Where(p => p.Team == player.Team && p.SteamID != player.SteamID && p.PawnIsAlive)
                .Any(p => SkillUtils.GetPlayerInfo(p)?.Skill == skillName);

            if (stillHasGlitch)
                return;

            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0) return;
            foreach (var enemy in enemies)
                enemy.ReplicateConVar("sv_disable_radar", "0");
        }
    }
}