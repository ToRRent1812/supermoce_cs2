using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Evade : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Evade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "CS2",
                "Szansa na uniknięcie trafienia",
                "#b2dd18",
                minValue: 10,
                maxValue: 30,
                step: 1,
                customValueFormatter: (value) => $"{value}%");
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
            playerInfo.SkillChance = 1f;
        }

        public static HookResult OnPlayerTakeDamage(CCSPlayerPawn victim, CTakeDamageInfo info)
        {
            if (victim == null || !victim.IsValid || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);

            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null || victim.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController victimController = victim.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(victimController);
            if (playerInfo == null) return HookResult.Continue;
            if (playerInfo?.Skill == skillName && victimController.PawnIsAlive && Instance?.Random.NextDouble() <= playerInfo.SkillChance)
            {
                info.Damage = 0f;
            }

            return HookResult.Continue;
        }
    }
}