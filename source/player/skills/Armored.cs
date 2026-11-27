using CounterStrikeSharp.API.Core;
using Supermoce.src.player;

namespace Supermoce
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

        public static HookResult OnPlayerTakeDamage(CCSPlayerPawn victim, CTakeDamageInfo info)
        {
            if (victim == null || !victim.IsValid || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);

            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null || victim.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victimController = victim.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && victimController.PawnIsAlive)
            {
                float? skillChance = playerInfo.SkillChance;
                info.Damage *= skillChance ?? 1f;
            }
            return HookResult.Continue;
        }
    }
}