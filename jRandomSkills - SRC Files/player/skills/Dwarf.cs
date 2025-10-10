using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dwarf : ISkill
    {
        private const Skills skillName = Skills.Dwarf;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Mini Majk", "Jesteś malutki", "#ffff00");
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                DisableSkill(player);
            }
        }

        public static unsafe void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && player.IsValid)
            {
                int randomValue = Instance?.Random?.Next(35,61) ?? 35; //35-60%
                playerInfo.SkillChance = randomValue / 100f;
                playerInfo.RandomPercentage = (100-randomValue).ToString() + "% mniejszy";

                SkillUtils.ChangePlayerScale(player, (float)playerInfo.SkillChance);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && playerPawn?.CBodyComponent != null)
            {
                playerInfo.SkillChance = 1f; 
                playerInfo.RandomPercentage = "";
                SkillUtils.ChangePlayerScale(player, 1f);
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            }
        }
    }
}