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
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.3f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill == skillName)
                            EnableSkill(player);
                    }
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
                if (players.Length <= 0) return HookResult.Continue;
                foreach (var player in players)
                    player.ReplicateConVar("sv_disable_radar", "0");

                return HookResult.Continue;
            });
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
            CheckOtherGlitchers();
        }

        private static void CheckOtherGlitchers()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player != null && player.IsValid)
                {
                    var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (player.PawnIsAlive && playerInfo?.Skill == skillName) return;
                    // Jeżeli ktoś nadal ma Glitch, to nie przywracamy radaru

                    var enemies = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum != player.TeamNum && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).ToArray();
                    if (enemies.Length <= 0) continue;
                    foreach (var enemy in enemies)
                        enemy.ReplicateConVar("sv_disable_radar", "0");
                }
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#f542ef", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
