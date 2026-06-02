using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class SecondLife : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.SecondLife;
        private static readonly ConcurrentDictionary<nint, byte> secondLifePlayers = new();
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Drugie życie",
                "Po śmierci odradzasz się 1 raz z losową ilością zdrowia",
                "#d41c1c",
                minValue: 10,
                maxValue: 100,
                step: 2,
                customValueFormatter: value => $"{value} HP");
        }

        public static void NewRound()
        {
            secondLifePlayers.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;

            if (victim == null || !victim.IsValid) return;
            var victimInfo = SkillUtils.GetPlayerInfo(victim);
            if (victimInfo == null || victimInfo.Skill != skillName) return;

            var victimPawn = victim!.PlayerPawn.Value;
            if (victimPawn == null || !victimPawn.IsValid) return;
            if (victimPawn.Health > 0 || secondLifePlayers.ContainsKey(victim.Handle))
                return;

            lock (setLock)
            {
                if (!secondLifePlayers.TryAdd(victim.Handle, 0))
                    return;

                victimPawn.Health = 1;
                Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");

                Instance?.AddTimer(0.1f, () =>
                {
                    if (victim == null || !victim.IsValid) return;
                    var pawn = victim.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid) return;

                    var config = SkillUtils.GetPassiveSkillConfig(skillName);
                    int revivalHealth = PassiveSkillFramework.GetRandomRoll(skillName, victim, config);
                    SetHealth(victim, revivalHealth);
                    var spawn = GetSpawnVector(victim);
                    if (spawn != null)
                    {
                        pawn.Teleport(spawn, pawn.AbsRotation, null);
                        SkillUtils.PrintToChat(victim, $"Drugie życie");
                    }
                });
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            secondLifePlayers.TryRemove(player.Handle, out _);
            PassiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        private static void SetHealth(CCSPlayerController player, int health)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            pawn.Health = health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            pawn.ArmorValue = health/2;
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