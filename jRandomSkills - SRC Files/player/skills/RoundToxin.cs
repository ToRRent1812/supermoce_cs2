using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;

namespace jRandomSkills
{
    public class RoundToxin : ISkill
    {
        private const Skills skillName = Skills.RoundToxin;
        private static readonly ConcurrentDictionary<ulong, byte> activePlayers = new ConcurrentDictionary<ulong, byte>();

        private const int AoERadius = 1000; // units

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Bąk morderca", "Wrogowie w twoim pobliżu tracą życie", "#4d2b2b");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            activePlayers.TryAdd(player.SteamID, 0);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            activePlayers.TryRemove(player.SteamID, out _);
        }

        public static void NewRound()
        {
            activePlayers.Clear();
        }

        private static void AddHealth(CCSPlayerPawn player, int health)
        {
            if (player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            player.Health += health;
            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");

            player.EmitSound("Player.DamageBody.Onlooker", volume: 0.1f);
            if (player.Health <= 0)
                player.CommitSuicide(false, true);
        }

        public static void OnTick()
        {
            if (Server.TickCount % 64 != 0) return;
            if (SkillUtils.IsFreezetime()) return;
            if (activePlayers.IsEmpty) return;

            var players = Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.PlayerPawn.Value != null && p.PlayerPawn.Value.IsValid && p.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE).ToArray();

            foreach (var ap in activePlayers.Keys)
            {
                var owner = players.FirstOrDefault(p => p.SteamID == ap);
                if (owner == null || owner.PlayerPawn.Value == null) continue;

                var ownerPos = owner.PlayerPawn.Value.AbsOrigin;
                if (ownerPos == null) continue;

                foreach (var target in players)
                {
                    if (target.SteamID == owner.SteamID) continue;
                    if (target.Team == owner.Team) continue;

                    var targetPawn = target.PlayerPawn.Value;
                    if (targetPawn == null || targetPawn.AbsOrigin == null) continue;

                    if (SkillUtils.GetDistance(ownerPos, targetPawn.AbsOrigin) <= AoERadius)
                    {
                        AddHealth(targetPawn, -1);
                    }
                }
            }
        }
    }
}
