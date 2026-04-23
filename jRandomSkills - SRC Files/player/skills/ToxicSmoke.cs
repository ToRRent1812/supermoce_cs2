using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ToxicSmoke : ISkill
    {
        private const Skills skillName = Skills.ToxicSmoke;
        private static readonly ConcurrentDictionary<Vector, CsTeam> smokes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Chemik", "Twoje smoke'i zadają wrogom obrażenia. Granat po wypaleniu wraca do ręki", "#507529");
        }

        public static void NewRound()
        {
            smokes.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.SmokeGrenade);
        }

        public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            smokes.TryAdd(new Vector(@event.X, @event.Y, @event.Z), player.Team);
        }

        public static void SmokegrenadeExpired(EventSmokegrenadeExpired @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            foreach (var smoke in smokes.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
                smokes.TryRemove(smoke, out _);
        }

        private static void AddHealth(CCSPlayerPawn player, int health)
        {
            if (player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            player.Health += health;
            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");

            player.EmitSound("Player.DamageBody.Onlooker", volume: 0.3f);
            if (player.Health <= 0)
                player.CommitSuicide(false, true);
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;

            foreach (var kv in smokes)
            {
                Vector smokePos = kv.Key;
                CsTeam ownerTeam = kv.Value;

                foreach (var player in Utilities.GetPlayers())
                {
                    if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                    if (player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE || player.PlayerPawn.Value.AbsOrigin == null) continue;
                    if (player.Team == CsTeam.Spectator) continue;

                    // only hurt opposing team
                    if (player.Team == ownerTeam) continue;

                    if (SkillUtils.GetDistance(smokePos, player.PlayerPawn.Value.AbsOrigin) <= 170)
                        AddHealth(player.PlayerPawn.Value, -7);
                }
            }
        }
    }
}