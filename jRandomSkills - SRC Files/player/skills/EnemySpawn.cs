using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class EnemySpawn : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.EnemySpawn;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Podchody",
                "Możesz przenieść się na resp wroga",
                "#ff8c92",
                minCooldown: 15,
                maxCooldown: 40,
                cooldownStep: 5);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (SkillUtils.IsFreezetime()) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);

            playerPawn.Teleport(GetEnemySpawnVector(player));
        }

        private static Vector GetEnemySpawnVector(CCSPlayerController player)
        {
            var abs = player!.PlayerPawn!.Value?.AbsOrigin;
            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(player.Team == CsTeam.CounterTerrorist ? "info_player_terrorist" : "info_player_counterterrorist").ToList();
            if (spawns.Count != 0)
            {
                var randomSpawn = spawns[(Instance?.Random.Next(spawns.Count)) ?? 1];
                if (randomSpawn.AbsOrigin != null)
                    return randomSpawn.AbsOrigin;
            }
            return abs == null ? new Vector(0, 0, 0) : new Vector(abs.X, abs.Y, abs.Z);
        }
    }
}