using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Phoenix : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Phoenix;
        private static readonly ConcurrentDictionary<nint, byte> phoenixPlayers = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Feniks",
                "% szans na odrodzenie się po śmierci z 50 HP",
                "#d4751c",
                minValue: 20,
                maxValue: 50,
                step: 1,
                customValueFormatter: (value) => $"{value}%");
        }

        public static void NewRound()
        {
            phoenixPlayers.Clear();
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

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(victim) == false) return;
            var victimInfo = SkillUtils.GetPlayerInfo(victim);
            if (victimInfo == null || victimInfo.Skill != skillName) return;

            var victimPawn = victim!.PlayerPawn.Value;
            if (victimPawn!.Health > 0 || phoenixPlayers.ContainsKey(victim.Handle))
                return;

            if (Instance?.Random.NextDouble() > victimInfo.SkillChance) return;

            lock (setLock)
            {
                phoenixPlayers.TryAdd(victim.Handle, 0);
                SetHealth(victim, 50);
                var spawn = GetSpawnVector(victim);
                if (spawn != null)
                {
                    victimPawn.Teleport(spawn, victimPawn.AbsRotation, null);
                }
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null) return;

            phoenixPlayers.TryRemove(player.Handle, out _);
            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo != null)
            {
                playerInfo.SkillChance = 0;
            }
        }

        private static void SetHealth(CCSPlayerController player, int health)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            pawn.Health = health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            pawn.ArmorValue = 100;
            SkillUtils.TryGiveWeapon(player, CsItem.AssaultSuit);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        private static Vector? GetSpawnVector(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return null;
            
            var abs = pawn.AbsOrigin;
            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(player.Team == CsTeam.Terrorist ? "info_player_terrorist" : "info_player_counterterrorist").ToList();
            if (spawns.Count != 0)
            {
                var randomSpawn = spawns[(Instance?.Random.Next(spawns.Count)) ?? 0];
                return randomSpawn.AbsOrigin;
            }
            return abs == null ? null : new Vector(abs.X, abs.Y, abs.Z);
        }
    }
}