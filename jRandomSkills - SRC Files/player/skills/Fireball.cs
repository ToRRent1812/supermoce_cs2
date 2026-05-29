using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Fireball : ISkill
    {
        private const Skills skillName = Skills.Fireball;
        //private static readonly ConcurrentDictionary<ulong, int> lastExplosionTick = [];
        private static readonly QAngle explosionAngle = new(77, 11, -22);

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Trotylov", "Twoje mołotowy wybuchają", "#e25d2d", 1);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }

        /*public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var weapon = @event.Weapon;

            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return;

            if (!(weapon == "inferno" || weapon == "molotov" || weapon == "incgrenade")) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (lastExplosionTick.TryGetValue(attacker.SteamID, out var last) && Server.TickCount - last < 64) return;
            lastExplosionTick.AddOrUpdate(attacker.SteamID, Server.TickCount, (k, v) => Server.TickCount);

            Vector pos;
            var victimPawn = victim.PlayerPawn.Value;
            if (victimPawn != null && victimPawn.IsValid && victimPawn.AbsOrigin != null)
                pos = victimPawn.AbsOrigin;
            else
            {
                var pawn = attacker.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.AbsRotation == null) return;
                pos = pawn.AbsOrigin + SkillUtils.GetForwardVector(pawn.AbsRotation) * 250f;
            }

            Server.NextFrame(() =>
            {
                SkillUtils.CreateHEGrenadeProjectile(pos, explosionAngle, new Vector(0, 0, 0), 0);
            });

            float radius = 400f;
            int damage = 150;

            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || p.PlayerPawn.Value == null || !p.PlayerPawn.Value.IsValid) continue;
                var pawn = p.PlayerPawn.Value;
                if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null) continue;

                if (SkillUtils.GetDistance(pos, pawn.AbsOrigin) <= radius)
                {
                    // allow damage to attacker (self) and to enemies only
                    if (p.SteamID != attacker.SteamID && p.TeamNum == attacker.TeamNum)
                        continue;

                    SkillUtils.TakeHealth(pawn, damage);
                }
            }
        }*/

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            Vector pos = new(@event.X, @event.Y, @event.Z+10);

            Server.NextFrame(() =>
            {
                SkillUtils.CreateHEGrenadeProjectile(pos, explosionAngle, new Vector(0, 0, -10), player.TeamNum);
            });
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (entity == null || entity.DesignerName != "hegrenade_projectile") return;

            var heProjectile = entity.As<CHEGrenadeProjectile>();
            if (heProjectile == null || !heProjectile.IsValid || heProjectile.AbsRotation == null) return;

            Server.NextFrame(() =>
            {
                if (heProjectile == null || !heProjectile.IsValid || heProjectile.AbsRotation == null) return;

                if (!(NearlyEquals(explosionAngle.X, heProjectile.AbsRotation.X) && NearlyEquals(explosionAngle.Y, heProjectile.AbsRotation.Y) && NearlyEquals(explosionAngle.Z, heProjectile.AbsRotation.Z)))
                    return;

                heProjectile.TicksAtZeroVelocity = 100;
                //heProjectile.TeamNum = (byte)CsTeam.None;
                heProjectile.Damage = 100f;
                heProjectile.DmgRadius = 400f;
                heProjectile.DetonateTime = 0;
            });
        }

        private static bool NearlyEquals(float a, float b, float epsilon = 0.001f) => Math.Abs(a - b) < epsilon;
    }
}
