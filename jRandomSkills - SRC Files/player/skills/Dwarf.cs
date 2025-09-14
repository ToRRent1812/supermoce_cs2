using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dwarf : ISkill
    {
        private const Skills skillName = Skills.Dwarf;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                DisableSkill(player);
            }
        }

        public static unsafe void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && player.IsValid)
            {
                float newSize = (float)Instance.Random.NextDouble() * (Config.GetValue<float>(skillName, "maxScale") - Config.GetValue<float>(skillName, "minScale")) + Config.GetValue<float>(skillName, "minScale");
                newSize = (float)Math.Round(newSize, 2);
                playerInfo.RandomPercentage = ((int)(newSize * 100)).ToString() + "%";
                playerInfo.SkillChance = newSize;

                SkillUtils.ChangePlayerScale(player, newSize);
                SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("dwarf")}{ChatColors.Lime}: " + Localization.GetTranslation("dwarf_desc2", newSize), false);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && playerPawn?.CBodyComponent != null)
            {
                playerInfo.SkillChance = 1;
                SkillUtils.ChangePlayerScale(player, 1);
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ffff00", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float minScale = .35f, float maxScale = .6f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float MinScale { get; set; } = minScale;
            public float MaxScale { get; set; } = maxScale;
        }
    }
}