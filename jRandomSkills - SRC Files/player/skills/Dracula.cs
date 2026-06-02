using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dracula : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Dracula;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Wampir",
                "% zadanych obrażeń zamieniasz w życie",
                "#FA050D",
                teamnum: 1,
                minValue: 5,
                maxValue: 30,
                step: 5,
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

            if (attacker == null || Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = SkillUtils.GetPlayerInfo(attacker);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                var config = SkillUtils.GetPassiveSkillConfig(skillName);
                if (config == null) return;

                int percent = PassiveSkillFramework.GetRandomRoll(skillName, attacker, config);
                HealAttacker(attacker, @event.DmgHealth, percent);
            }
        }

        private static void HealAttacker(CCSPlayerController attacker, float damage, int percent)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            if (attackerPawn == null) return;

            int healedAmount = Math.Max(1, (int)Math.Round(damage * percent / 100f));
            int newHealth = attackerPawn.Health + healedAmount;

            attackerPawn.MaxHealth = Math.Max(newHealth, 100);
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iMaxHealth");

            attackerPawn.Health = newHealth;
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
        }
    }
}
