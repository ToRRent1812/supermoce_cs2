using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class TimeManipulator : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.TimeManipulator;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Matrix",
                "Możesz spowolnić czasoprzestrzeń",
                "#2ec761",
                minCooldown: 15,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            if (Instance?.GameRules?.FreezePeriod == true) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);

            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("host_timescale 0.25");
            Instance?.AddTimer(.9f, () =>
            {
                Server.ExecuteCommand("host_timescale 1");
                Server.ExecuteCommand("sv_cheats 0");
            });
        }
    }
}
