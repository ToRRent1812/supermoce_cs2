using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Glitch : ISkill
    {
        private const Skills skillName = Skills.Glitch;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Anty-Radar", "Przeciwnicy grają rundę bez radaru", "#f542ef");
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
            var enemies = Utilities.GetPlayers().Where(p => p.Team != player.Team && p.IsValid && p.PawnIsAlive && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
            if (enemies.Length <= 0) return;
            foreach (var enemy in enemies)
                enemy.ReplicateConVar("sv_disable_radar", "1");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            foreach (var oplayer in Utilities.GetPlayers())
            {
                if (oplayer != null && oplayer.IsValid)
                {
                    var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == oplayer.SteamID);
                    if (oplayer.PawnIsAlive && playerInfo?.Skill == skillName) return;
                    // Jeżeli ktoś nadal ma Glitch, to nie przywracamy radaru

                    var enemies = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum != oplayer.TeamNum && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
                    if (enemies.Length <= 0) continue;
                    foreach (var enemy in enemies)
                        enemy.ReplicateConVar("sv_disable_radar", "0");
                }
            }
        }
    }
}