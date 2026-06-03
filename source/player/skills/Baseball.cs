using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Collections.Concurrent;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Baseball : ISkill
    {
        private const Skills skillName = Skills.Baseball;
        private static readonly ConcurrentDictionary<uint, byte> decoys = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName, 
                "Baseball", 
                "Twoje wabiki odbijają się od ścian. Trafienie nim zabija", 
                "#49ff67");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            var weapon = @event.Weapon;

            if (weapon != "decoy") return;
            if (Instance == null || !Instance.IsPlayerValid(victim) || !Instance.IsPlayerValid(attacker)) return;

            var attackerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (attackerInfo?.Skill != skillName) return;

            SkillUtils.TakeHealth(victim!.PlayerPawn.Value, 999);
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            var name = entity.DesignerName;
            if (name != "decoy_projectile")
                return;

            var decoy = entity.As<CDecoyProjectile>();
            if (decoy == null || !decoy.IsValid || decoy.OwnerEntity == null || decoy.OwnerEntity.Value == null || !decoy.OwnerEntity.Value.IsValid) return;

            var pawn = decoy.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            decoys.TryAdd(decoy.Index, 0);

            decoy.Collision.CollisionAttribute.InteractsWith = pawn.Collision.CollisionAttribute.InteractsWith;
            decoy.Collision.CollisionGroup = pawn.Collision.CollisionGroup;
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            uint key = (uint)@event.Entityid;
            if (decoys.ContainsKey(key))
            {
                var decoy = Utilities.GetEntityFromIndex<CDecoyProjectile>(@event.Entityid);
                if (decoy != null && decoy.IsValid)
                    decoy.AcceptInput("Kill");
                decoys.TryRemove(key, out _);
            }
        }

        public static void OnTick()
        {
            if (Server.TickCount % 8 != 0) return;

            var keys = decoys.Keys.ToArray();

            foreach (var decoyIndex in keys)
            {
                var decoy = Utilities.GetEntityFromIndex<CDecoyProjectile>((int)decoyIndex);

                if (decoy == null || !decoy.IsValid)
                {
                    decoys.TryRemove(decoyIndex, out _);
                    continue;
                }

                decoy.Bounces = 0;
                
                var vel = decoy.AbsVelocity;
                float speed = vel.Length();
                float targetSpeed = Math.Min(speed * 3f, 1000f);

                if (speed > .01f)
                {
                    var dir = vel / speed;
                    var newVelocity = dir * targetSpeed;

                    decoy.AbsVelocity.X = newVelocity.X;
                    decoy.AbsVelocity.Y = newVelocity.Y;
                    decoy.AbsVelocity.Z = newVelocity.Z;
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }
    }
}