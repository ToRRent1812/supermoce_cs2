using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Astronaut : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Astronaut;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Astronauta",
                "Masz niską grawitację",
                "#7E10AD",
                minValue: 25,
                maxValue: 60,
                step: 5,
                customValueFormatter: (value) => $"{100 - value}% mniejsza");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);

                int randomRoll = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                playerInfo.SkillChance = randomRoll / 100f;
                player.PlayerPawn.Value.ActualGravityScale = playerInfo.SkillChance ?? 1f;
            }
        }

        public static void NewRound()
        {
            foreach (var player in SkillUtils.CachedPlayers)
                DisableSkill(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            PassiveSkillFramework.OnSkillDisabled(skillName, player);
            
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            player.PlayerPawn.Value.ActualGravityScale = 1f;
            playerInfo.SkillChance = 1f;
        }
    }
}