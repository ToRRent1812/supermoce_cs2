using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class Armored : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Armored;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Ulaniec",
                "Dostajesz % mniej obrażeń",
                "#d1430a",
                minValue: 50,
                maxValue: 85,
                step: 5,
                customValueFormatter: (value) => $"{100 - value}% mniej");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
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

        public static void DisableSkill(CCSPlayerController player)
        {
            PassiveSkillFramework.OnSkillDisabled(skillName, player);
            
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo != null)
            {
                playerInfo.SkillChance = 1f;
            }
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && victim.PawnIsAlive)
            {
                float? skillChance = playerInfo.SkillChance;
                info.Damage *= skillChance ?? 1f;
            }
            return HookResult.Continue;
        }
    }
}