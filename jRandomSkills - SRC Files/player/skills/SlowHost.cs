using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class SlowHost : ISkill
    {
        private const Skills skillName = Skills.SlowHost;
        private static bool skillExists = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ciężarne hosty", "Póki jesteś żywy, chodzenie z hostem jest znacznie wolniejsze.", "#fd4371", 1);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null || playerInfo?.Skill != skillName) return;
            skillExists = true;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid) return;
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid)
                {
                    var playerInfo = Instance?.SkillPlayer.FirstOrDefault(a => a.SteamID == p.SteamID);
                    if (p.PawnIsAlive && playerInfo?.Skill == skillName) return;
                }
            }
            skillExists = false;
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
            if(skillExists) playerPawn.VelocityModifier *= 0.5f;
        }
    }
}