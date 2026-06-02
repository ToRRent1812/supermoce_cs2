using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Retreat : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Retreat;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Ucieczka",
                "Powrót na spawn",
                "#a86eff",
                minCooldown: 15,
                maxCooldown: 45,
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
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player)) return;

            var spawnPosition = GetSpawnVector(player);
            if (spawnPosition == null) return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            playerPawn.Teleport(spawnPosition);
        }

        private static Vector? GetSpawnVector(CCSPlayerController player)
        {
            if (player == null) return null;

            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(player.Team == CsTeam.Terrorist ? "info_player_terrorist" : "info_player_counterterrorist").ToList();
            if (spawns.Count == 0) return null;

            var randomIndex = Instance?.Random.Next(spawns.Count) ?? 0;
            return spawns[randomIndex].AbsOrigin;
        }
    }
}
