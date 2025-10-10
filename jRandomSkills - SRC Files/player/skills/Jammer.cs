using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Jammer : ISkill
    {
        private const Skills skillName = Skills.Jammer;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ślepy knur", "Przeciwnicy całą rundę grają bez celownika", "#42f5a7");
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
            var enemies = Utilities.GetPlayers().Where(p => p.Team != player.Team && p.IsValid && p.PawnIsAlive && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
            if (enemies.Length > 0)
            {
                foreach (var enemy in enemies)
                    SetCrosshair(enemy, false);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            BringBackCrosshair();
        }

        private static void BringBackCrosshair()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player != null && player.IsValid)
                {
                    var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (player.PawnIsAlive && playerInfo?.Skill == skillName) return;
                    // Jeżeli ktoś nadal ma knura, to nie przywracamy celownika

                    var enemies = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum != player.TeamNum && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
                    if (enemies.Length <= 0) continue;
                    foreach (var enemy in enemies)
                        SetCrosshair(enemy, true);
                }
            }
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