using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class MegaMoly : ISkill
    {
        private const Skills skillName = Skills.MegaMoly;
        private static readonly ConcurrentDictionary<Vector, ulong> fires = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Mega-Tov", "Twoje mołotowy mocniej rozprzestrzeniają ogień", "#ff7b00", 1);
        }

        public static void NewRound()
        {
            fires.Clear();
        }

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(player.SteamID, out var skillPlayer) ? skillPlayer : null;
            if (playerInfo?.Skill != skillName) return;
            fires.TryAdd(new Vector(@event.X, @event.Y, @event.Z), player.SteamID);
            Instance?.AddTimer(20.0f, () =>
            {
                SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
            });
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            var inferno = entity.As<CInferno>() ?? new CInferno(entity.Handle);
            if (inferno == null || !inferno.IsValid)
            {
                return;
            }

            // In CS2, CInferno.OwnerEntity points directly to the CCSPlayerPawn, not the projectile
            CCSPlayerPawn? pawn = null;
            
            if (inferno.OwnerEntity != null && inferno.OwnerEntity.IsValid && inferno.OwnerEntity.Value != null && inferno.OwnerEntity.Value.IsValid)
            {
                pawn = inferno.OwnerEntity.Value.As<CCSPlayerPawn>();
            }

            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid)
            {
                return;
            }

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid)
            {
                return;
            }

            var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(player.SteamID, out var skillPlayer) ? skillPlayer : null;
            if (playerInfo?.Skill != skillName)
            {
                return;
            }

            if (inferno.IsValid)
            {
                inferno.MaxFlames = 40;
                inferno.FireCount = 40;
                inferno.SpreadCount = 40;
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }
    }
}