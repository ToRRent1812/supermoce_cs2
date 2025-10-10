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
        private static readonly ConcurrentDictionary<Vector, byte> smokes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Chemik", "Twoje smoke'i zadają obrażenia. Granat po wypaleniu wraca do ręki", "#507529");
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
            smokes.TryAdd(new Vector(@event.X, @event.Y, @event.Z), 0);
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
            foreach (Vector smokePos in smokes.Keys)
                foreach (var player in Utilities.GetPlayers())
                    if (Server.TickCount % 32 == 0)
                        if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                            if (player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && player.PlayerPawn.Value.AbsOrigin != null)
                                if (SkillUtils.GetDistance(smokePos, player.PlayerPawn.Value.AbsOrigin) <= 170)
                                    AddHealth(player.PlayerPawn.Value, -7);
        }
    }
}