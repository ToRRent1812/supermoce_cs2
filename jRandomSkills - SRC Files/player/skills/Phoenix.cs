using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Phoenix : ISkill
    {
        private const Skills skillName = Skills.Phoenix;
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Feniks", "Jeżeli nie jesteś ostatni żywy w drużynie, masz szansę odrodzić się po śmierci", "#ff5C0A", 2);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
            {
                int aliveCount = Utilities.GetPlayers().Count(p => p.TeamNum == player.TeamNum && p.PawnHealth > 0);
                if (Instance?.Random.NextDouble() <= playerInfo.SkillChance && aliveCount > 1)
                {
                    lock (setLock)
                    {
                        player.Respawn();
                        Instance?.AddTimer(.2f, player.Respawn);
                    }
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(6,14) * 5 ?? 10; //30-70%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }
    }
}