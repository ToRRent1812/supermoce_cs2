using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class LongFlame : ISkill
    {
        private const Skills skillName = Skills.LongFlame;
        private static readonly ConcurrentDictionary<Vector, ulong> fires = [];
        private static readonly ConcurrentDictionary<uint, float> infernoEndTimes = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Ognisko", 
            "Twoje mołotowy palą się 60 sek.", 
            "#ff3c00", 
            teamnum:1);
        }

        public static void NewRound()
        {
            fires.Clear();
            infernoEndTimes.Clear();
        }

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            fires.TryAdd(new Vector(@event.X, @event.Y, @event.Z), player.SteamID);
            Instance?.AddTimer(20.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
            });
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (entity.DesignerName != "inferno")
                return;

            var inferno = entity.As<CInferno>();
            if (inferno == null || !inferno.IsValid)
                return;

            if (inferno.OwnerEntity == null || !inferno.OwnerEntity.IsValid || inferno.OwnerEntity.Value == null || !inferno.OwnerEntity.Value.IsValid)
                return;

            var pawn = inferno.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid)
                return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName)
                return;

            infernoEndTimes[inferno.Index] = Server.CurrentTime + 60f;
        }

        public static void OnTick()
        {
            if (Server.TickCount % 16 != 0) return;

            var now = Server.CurrentTime;
            var expired = new List<uint>();

            foreach (var kv in infernoEndTimes)
            {
                var inferno = Utilities.GetEntityFromIndex<CInferno>((int)kv.Key);
                if (inferno == null || !inferno.IsValid)
                {
                    expired.Add(kv.Key);
                    continue;
                }

                if (now >= kv.Value)
                {
                    expired.Add(kv.Key);
                    continue;
                }

                if (inferno.FireCount < 5)
                    inferno.FireCount = 5;
            }

            foreach (var index in expired)
                infernoEndTimes.TryRemove(index, out _);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }
    }
}
