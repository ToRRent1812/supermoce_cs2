using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Push : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Push;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Śmierdziel",
                "Szansa na odsunięcie wroga po trafieniu",
                "#1e9ab0",
                minValue: 10,
                maxValue: 30,
                step: 5,
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
                int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                playerInfo.SkillChance = randomValue / 100f;
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 0f;
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim)
                return;

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null || playerInfo.Skill != skillName || !victim!.PawnIsAlive)
                return;

            if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                PushEnemy(victim, attacker!.PlayerPawn.Value!.EyeAngles);
        }

        private static void PushEnemy(CCSPlayerController player, QAngle attackerAngle)
        {
            if (player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            var currentPosition = player.PlayerPawn.Value.AbsOrigin;
            var currentAngles = player.PlayerPawn.Value.EyeAngles;

            Vector newVelocity = SkillUtils.GetForwardVector(attackerAngle) * 500f;
            newVelocity.Z = player.PlayerPawn.Value.AbsVelocity.Z + 200f;

            player.PlayerPawn.Value.Teleport(currentPosition, currentAngles, newVelocity);
        }
    }
}