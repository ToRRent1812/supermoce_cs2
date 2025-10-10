using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ReturnToSender : ISkill
    {
        private const Skills skillName = Skills.ReturnToSender;
        private static readonly ConcurrentDictionary<nint, byte> playersToSender = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Zwrot do Nadawcy", "Trafienie wroga cofa go 1 raz na resp", "#a68132");
        }

        public static void NewRound()
        {
            playersToSender.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            int damage = @event.DmgHealth;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
            if (attackerInfo == null || attackerInfo.Skill != skillName) return;

            if (playersToSender.TryGetValue(victim!.Handle, out _))
                return;

            var spawn = GetSpawnVector(victim);
            if (spawn == null) return;
            victim!.PlayerPawn!.Value!.Teleport(spawn);
            playersToSender.TryAdd(victim.Handle, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersToSender.TryRemove(player.Handle, out _);
        }

        private static Vector? GetSpawnVector(CCSPlayerController player)
        {
            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(player.Team == CsTeam.Terrorist ? "info_player_terrorist" : "info_player_counterterrorist").ToList();
            if (spawns.Count != 0)
            {
                var randomSpawn = spawns[(Instance?.Random.Next(spawns.Count)) ?? 0];
                return randomSpawn.AbsOrigin;
            }
            return null;
        }
    }
}