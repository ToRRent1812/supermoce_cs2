using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class InstantEscape : ISkill
    {
        private const Skills skillName = Skills.InstantEscape;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Owsiak", 
            "Wzięcie hosta wygrywa rundę", 
            "#30fa7d", 
            teamnum:2, 
            objective:2);
        }

        public static void HostageFollows(EventHostageFollows @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null || playerInfo.Skill != skillName) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
            SkillUtils.AwardRoundEndMoney(CsTeam.CounterTerrorist, player, RoundEndReason.AllHostageRescued);
            SkillUtils.PrintToChatAll($" {ChatColors.LightBlue}{player.PlayerName} {ChatColors.Lime}wygrał rundę mając moc {ChatColors.DarkRed}Owsiak", false);
        }
    }
}