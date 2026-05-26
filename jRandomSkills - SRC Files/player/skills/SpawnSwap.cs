using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class SpawnSwap : ISkill
    {
        private const Skills skillName = Skills.SpawnSwap;
        private static bool isSwapped = false;
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Zamiana respów", "Drużyny zamieniają się respami", "#ffe600", 1, 1);
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                isSwapped = false;
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            lock (setLock)
            {
                if (isSwapped) return;
                isSwapped = true;
            }
            Server.ExecuteCommand("mp_solid_teammates 0");
            Instance?.AddTimer(5.0f, () =>
            {
                foreach(var p in Utilities.GetPlayers())
                {
                    if (p != null && p.IsValid && p.PawnIsAlive && p.PawnHealth > 0)
                    {
                        var playerPawn = p.PlayerPawn.Value;
                        if (playerPawn == null) return;
                        playerPawn.Teleport(GetEnemySpawnVector(p));
                    }
                }
            });
            Instance?.AddTimer(9.0f, () =>
            {
                Server.ExecuteCommand("mp_solid_teammates 1");
            });
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