using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Dwarf : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Dwarf;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Mini Majk",
                "Jesteś malutki",
                "#ffff00",
                minValue: 40,
                maxValue: 80,
                step: 5,
                customValueFormatter: (value) => $"{100 - value}% mniejszy");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);

                int randomRoll = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                playerInfo.SkillChance = randomRoll / 100f;

                SkillUtils.ChangePlayerScale(player, (float)playerInfo.SkillChance);
            }
        }

        public static void NewRound()
        {
            foreach (var player in SkillUtils.CachedPlayers)
                DisableSkill(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            SkillUtils.ChangePlayerScale(player, 1f);
            playerInfo.SkillChance = 1f;
        }
    }
}