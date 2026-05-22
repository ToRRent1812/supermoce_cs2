using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class InstantEscape : ISkill
    {
        private const Skills skillName = Skills.InstantEscape;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Owsiak", "Wzięcie hosta wygrywa rundę", "#30fa7d", 2);
        }

        public static void HostageFollows(EventHostageFollows @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null || playerInfo.Skill != skillName) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
            SkillUtils.PrintToChatAll($" {ChatColors.LightBlue}{player.PlayerName} wygrał rundę mając moc {ChatColors.DarkRed}Owsiak", false);
        }
    }
}