using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Soldier : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Soldier;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Oficer",
                "Zadajesz więcej obrażeń",
                "#00ba3e",
                minValue: 15,
                maxValue: 100,
                step: 5,
                customValueFormatter: value => $"+{value}%");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
            int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 1f + randomValue / 100f;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 1f;
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && attacker.PawnIsAlive)
            {
                float? skillChance = playerInfo.SkillChance;
                info.Damage *= skillChance ?? 1f;
            }

            return HookResult.Continue;
        }
    }
}