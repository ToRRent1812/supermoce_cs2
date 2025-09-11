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
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.3f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill == skillName)
                        {
                            EnableSkill(player);
                        }
                    }
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
                if (players.Length <= 0) return HookResult.Continue;
                foreach (var player in players)
                    SetCrosshair(player, true);

                return HookResult.Continue;
            });
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
                    var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#42f5a7", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
