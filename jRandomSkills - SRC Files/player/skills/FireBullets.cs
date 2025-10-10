using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FireBullets : ISkill
    {
        private const Skills skillName = Skills.FireBullets;
        private static readonly ConcurrentDictionary<CCSPlayerPawn, int> InfectedPlayers = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Trujące Ammo", "Twoje pociski trują przeciwników przez 5 sek.", "#23771c");
        }

        public static void NewRound()
        {
            lock (setLock)
                InfectedPlayers.Clear();
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
                InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (victim == null || attacker == null || victim.PlayerPawn.Value == null || victim.IsBot || !victim.IsValid || !victim.PawnIsAlive || Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false)
                return;
            if (attacker == victim || attacker.TeamNum == victim.TeamNum)
                return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo?.Skill != skillName) return;

            InfectedPlayers.AddOrUpdate(victim.PlayerPawn.Value, Server.TickCount, (k, v) => Server.TickCount);
        }

        public static void OnTick()
        {
            if(Server.TickCount % 64 != 0) return;
            foreach (var player in Utilities.GetPlayers().FindAll(p => p != null && p.PawnIsAlive))
            {
                if(player == null || player.PlayerPawn.Value == null) continue;
                if (InfectedPlayers.TryGetValue(player.PlayerPawn.Value, out int hitTick))
                {
                    if (player == null || !player.IsValid || !player.PawnIsAlive) continue;

                    if (Server.TickCount - hitTick >= 64 * 5)
                    {
                        InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _);
                        continue;
                    }
                    else
                    {
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn == null || !playerPawn.IsValid) continue;
                        SkillUtils.TakeHealth(playerPawn, Instance?.Random.Next(2, 7) ?? 2);
                        playerPawn.EmitSound("Player.DamageBody.Onlooker", volume: 0.25f);
                        if (playerPawn.Health <= 0)
                        {
                            InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _);
                            continue;
                        }
                    }
                }
            }
        }
    }
}
