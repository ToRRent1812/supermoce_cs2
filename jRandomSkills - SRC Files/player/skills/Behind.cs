using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Behind : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Behind;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Przekręcenie",
                "Szansa na obrócenie wroga po trafieniu",
                "#00FF00",
                minValue: 10,
                maxValue: 25,
                step: 5,
                customValueFormatter: (value) => $"{value}%");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event!.Attacker;
            var victim = @event!.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = SkillUtils.GetPlayerInfo(attacker);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
                if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                    RotateEnemy(victim);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                // Framework generates random value and updates display
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);

                // Retrieve the pre-generated random roll from framework
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
                playerInfo.SkillChance = 0;
            }
        }

        private static void RotateEnemy(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            var pawn = player.PlayerPawn.Value;

            if (pawn == null || !pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

            var currentPosition = pawn.AbsOrigin;
            var currentAngles = pawn.EyeAngles;

            QAngle newAngles = new(
                currentAngles.X,
                currentAngles.Y + 180,
                currentAngles.Z
            );

            pawn.Teleport(currentPosition, newAngles, new Vector(0, 0, 0));
        }
    }
}