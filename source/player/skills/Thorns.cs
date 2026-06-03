using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Thorns : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Thorns;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Lustro",
                "% otrzymanych obrażeń odczuje też przeciwnik",
                "#962631",
                minValue: 5,
                maxValue: 20,
                step: 1,
                customValueFormatter: value => $"{value}%");
        }

        public static void NewRound()
        {
            PassiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            if (attacker == null || victim == null) return;

            var victimInfo = SkillUtils.GetPlayerInfo(victim);
            if (victimInfo?.Skill != skillName || !victim.PawnIsAlive || !attacker.PawnIsAlive) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            int reflectPercent = PassiveSkillFramework.GetRandomRoll(skillName, victim, config);
            if (reflectPercent <= 0) return;

            int reflectedDamage = Math.Max(1, (int)Math.Round(@event.DmgHealth * reflectPercent / 100f));
            SkillUtils.TakeHealth(attacker.PlayerPawn.Value, reflectedDamage, false);
        }
    }
}
