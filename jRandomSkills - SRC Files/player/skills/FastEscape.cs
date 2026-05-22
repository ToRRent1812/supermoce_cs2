using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FastEscape : ISkill
    {
        private const Skills skillName = Skills.FastEscape;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Szybka pomoc", "Z hostem poruszasz się szybciej", "#1279ff", 2, 2);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            playerPawn.VelocityModifier = 1f;
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
            playerPawn.VelocityModifier *= 3f;
        }
    }
}