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
            // Try to treat the spawned entity as an inferno first. Some map/entity setups
            // may use intermediate entities (instanced_scripted_scene, etc.), so
            // relying solely on DesignerName can be noisy and unreliable.
            var inferno = entity.As<CInferno>() ?? new CInferno(entity.Handle);
            if (inferno == null || !inferno.IsValid)
            {
                // Not an inferno instance — ignore silently instead of noisy logging.
                return;
            }

            // Resolve the grenade projectile that caused this inferno. Some maps
            // insert an intermediate entity between the inferno and the projectile,
            // so try a couple of hops to find the actual projectile.
            CBaseCSGrenadeProjectile? projectile = null;

            if (inferno.OwnerEntity != null && inferno.OwnerEntity.IsValid && inferno.OwnerEntity.Value != null && inferno.OwnerEntity.Value.IsValid)
            {
                projectile = inferno.OwnerEntity.Value.As<CBaseCSGrenadeProjectile>();

                if (projectile == null || !projectile.IsValid)
                {
                    // Check one level deeper in case of an intermediate wrapper entity.
                    var ownerEntity = inferno.OwnerEntity.Value;
                    if (ownerEntity.OwnerEntity != null && ownerEntity.OwnerEntity.IsValid && ownerEntity.OwnerEntity.Value != null && ownerEntity.OwnerEntity.Value.IsValid)
                    {
                        projectile = ownerEntity.OwnerEntity.Value.As<CBaseCSGrenadeProjectile>();
                    }
                }
            }

            if (projectile == null || !projectile.IsValid || projectile.OwnerEntity == null || !projectile.OwnerEntity.IsValid || projectile.OwnerEntity.Value == null || !projectile.OwnerEntity.Value.IsValid)
            {
                Server.PrintToConsole("[MegaMoly] Projectile is invalid");
                return;
            }

            var pawn = projectile.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid)
            {
                Server.PrintToConsole("[MegaMoly] Pawn or Controller is invalid");
                return;
            }

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid)
            {
                Server.PrintToConsole("[MegaMoly] Player is invalid");
                return;
            }

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName)
            {
                Server.PrintToConsole($"[MegaMoly] Player skill doesn't match. Expected: {skillName}, Got: {playerInfo?.Skill}");
                return;
            }

            if (inferno.IsValid)
            {
                Server.PrintToConsole($"[MegaMoly] Default values - MaxFlames: {inferno.MaxFlames} | FireCount: {inferno.FireCount} | SpreadCount: {inferno.SpreadCount}");
                inferno.MaxFlames = 40;
                inferno.FireCount = 40;
                inferno.SpreadCount = 40;
                Server.PrintToConsole($"[MegaMoly] Updated values - MaxFlames: {inferno.MaxFlames} | FireCount: {inferno.FireCount} | SpreadCount: {inferno.SpreadCount}");
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }
    }
}