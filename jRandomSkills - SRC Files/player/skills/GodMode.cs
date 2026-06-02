using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class GodMode : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.GodMode;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Nieśmiertelka",
                "Chwilowo stajesz się nieśmiertelny",
                "#e0d83a",
                minCooldown: 25,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
            {
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);

            SkillUtils.PrintToChat(player, "Nieśmiertelność włączona", false);
            player.PlayerPawn.Value.TakesDamage = false;

            Instance?.AddTimer(1.5f, () => {
                if (player.IsValid && player.PawnIsAlive)
                {
                    player.PlayerPawn.Value.TakesDamage = true;
                    SkillUtils.PrintToChat(player, "Nieśmiertelność wyłączona", true);
                }
            });
        }
    }
}