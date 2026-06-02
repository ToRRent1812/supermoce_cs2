using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dopamine : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Dopamine;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Dopaminka",
                "Możesz przyspieszyć życie na serwerze",
                "#FA050D",
                minCooldown: 20,
                maxCooldown: 45,
                cooldownStep: 5);
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
            if (Instance?.GameRules?.FreezePeriod == true || !player.IsValid) return;
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);

            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("host_timescale 2");
            Instance?.AddTimer(4.5f, () =>
            {
                Server.ExecuteCommand("host_timescale 1");
                Server.ExecuteCommand("sv_cheats 0");
            });
        }
    }
}