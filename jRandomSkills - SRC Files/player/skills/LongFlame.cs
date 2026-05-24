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

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ognisko", "Twoje mołotowy płoną 60 sekund. Po 20 sekundach moli wraca do ręki.", "#ff8800", 1);
        }

        public static void NewRound()
        {
            fires.Clear();
        }

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
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
            if (entity.DesignerName != "inferno") return;

            var inferno = new CInferno(entity.Handle);
            if (inferno == null || !inferno.IsValid) return;

            if (inferno.OwnerEntity == null || !inferno.OwnerEntity.IsValid || inferno.OwnerEntity.Value == null || !inferno.OwnerEntity.Value.IsValid) return;

            var projectile = inferno.OwnerEntity.Value.As<CBaseCSGrenadeProjectile>();
            if (projectile == null || !projectile.IsValid || projectile.OwnerEntity == null || !projectile.OwnerEntity.IsValid || projectile.OwnerEntity.Value == null || !projectile.OwnerEntity.Value.IsValid) return;

            var pawn = projectile.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            Server.NextFrame(() =>
            {
                if (!inferno.IsValid) return;
                inferno.FireLifetime = 60f;
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }
    }
}