using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;
namespace Supermoce
{
    public class HighJump : ISkill
    {
        private const Skills skillName = Skills.HighJump;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(skillName,
            "Wąski",
            "Wyżej skaczesz",
            "#49b5e7",
            minValue: 20,
            maxValue: 75,
            step: 5,
            customValueFormatter: (value) => $"+{value}%");
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
            }
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if (player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            if (player.PlayerPawn?.Value == null) return;
            if (player.PawnIsAlive == false) return;
            Server.NextFrame(() =>
            {
                player.PlayerPawn.Value.BaseVelocity.Add(new Vector(z: 100 * playerInfo.SkillChance));
            });
        }
    }
}
