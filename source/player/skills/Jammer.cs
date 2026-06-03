using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Jammer : ISkill
    {
        private const Skills skillName = Skills.Jammer;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Ślepy knur", 
            "Przeciwnicy całą rundę grają bez celownika", 
            "#42f5a7");
        }

        public static void NewRound()
        {
            var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
            if (players.Length <= 0) return;
            foreach (var player in players)
                SetCrosshair(player, true);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length > 0)
            {
                foreach (var enemy in enemies)
                    SetCrosshair(enemy, false);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            bool stillHasJammer = SkillUtils.CachedPlayers
                .Where(p => p.Team == player.Team && p.SteamID != player.SteamID && p.PawnIsAlive)
                .Any(p => SkillUtils.GetPlayerInfo(p)?.Skill == skillName);

            if (stillHasJammer)
                return;

            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0) return;

            foreach (var enemy in enemies)
                SetCrosshair(enemy, true);
        }

        private static void SetCrosshair(CCSPlayerController player, bool enabled)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            pawn.HideHUD = (uint)(enabled
                ? (pawn.HideHUD & ~(1 << 8))
                : (pawn.HideHUD | (1 << 8)));
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_iHideHUD");
        }
    }
}