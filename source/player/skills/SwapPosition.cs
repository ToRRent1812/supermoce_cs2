using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class SwapPosition : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.SwapPosition;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Magik",
                "Zamieniasz się miejscem z losowym wrogiem",
                "#1466F5",
                minCooldown: 20,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
            {
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            if (SkillUtils.IsFreezetime()) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config == null) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player)) return;

            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0)
    {
        SkillUtils.PrintToChat(player, $"Nie znaleziono odpowiedniego wroga!", true);
        return;
    }

            var randomEnemy = enemies[(Instance?.Random.Next(enemies.Length)) ?? 0];
            if (!randomEnemy.IsValid || !randomEnemy.PawnIsAlive) return;

            var playerPawn = player.PlayerPawn?.Value;
            var enemyPawn = randomEnemy.PlayerPawn?.Value;
            if (playerPawn == null || !playerPawn.IsValid || enemyPawn == null || !enemyPawn.IsValid) return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            TeleportPlayers(player, randomEnemy);
        }

        private static void TeleportPlayers(CCSPlayerController attacker, CCSPlayerController victim)
        {
            var attackerPawn = attacker.PlayerPawn?.Value;
            var victimPawn = victim.PlayerPawn?.Value;
            if (attackerPawn == null || !attackerPawn.IsValid || victimPawn == null || !victimPawn.IsValid) return;
            if (attackerPawn.AbsOrigin == null || attackerPawn.AbsRotation == null || victimPawn.AbsOrigin == null || victimPawn.AbsRotation == null) 
            {
                SkillUtils.PrintToChat(attacker, $"Nie można znaleźć odpowiedniego wroga!");
                return;
            }

            var attackerPosition = new Vector(attackerPawn.AbsOrigin.X, attackerPawn.AbsOrigin.Y, attackerPawn.AbsOrigin.Z);
            var attackerAngles = new QAngle(attackerPawn.AbsRotation.X, Instance?.Random.Next(10, 350) ?? 10, attackerPawn.AbsRotation.Z);
            var attackerVelocity = new Vector(attackerPawn.AbsVelocity.X, attackerPawn.AbsVelocity.Y, attackerPawn.AbsVelocity.Z);

            var victimPosition = new Vector(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z);
            var victimAngles = new QAngle(victimPawn.AbsRotation.X, Instance?.Random.Next(10, 350) ?? 10, victimPawn.AbsRotation.Z);
            var victimVelocity = new Vector(victimPawn.AbsVelocity.X, victimPawn.AbsVelocity.Y, victimPawn.AbsVelocity.Z);

            victimPawn.Teleport(attackerPosition, attackerAngles, attackerVelocity);
            attackerPawn.Teleport(victimPosition, victimAngles, victimVelocity);
        }
    }
}
