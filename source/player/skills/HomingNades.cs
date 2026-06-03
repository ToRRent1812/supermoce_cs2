using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using Supermoce.src.player;

namespace Supermoce
{
    public class HomingNades : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.HomingNades;

        private static readonly ConcurrentDictionary<uint, Vector> trackedNades = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Pole magnetyczne", 
            "Granaty przyciągają się do wrogów (za wyjątkiem smoke)", 
            "#ff00aa");
        }

        public static void NewRound()
        {
            trackedNades.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.HEGrenade);
        }

        public static void OnTick()
        {
            if (trackedNades.IsEmpty) return;
            if (Server.TickCount % 16 != 0) return;

            foreach (var index in trackedNades.Keys)
            {
                if (!trackedNades.TryGetValue(index, out var lastPos)) continue;

                var nade = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)index);
                if (nade == null || !nade.IsValid || nade.AbsOrigin == null)
                {
                    trackedNades.TryRemove(index, out _);
                    continue;
                }

                Vector currentPos = nade.AbsOrigin;
                double distanceMoved = SkillUtils.GetDistance(currentPos, lastPos);
                Vector pushForce = CalculatePushForce(nade, nade.TeamNum);

                bool stopped = pushForce.IsZero();

                if (distanceMoved < 4f || stopped)
                {
                    nade.DetonateTime = stopped ? 0 : nade.CreateTime + 3f;
                    Utilities.SetStateChanged(nade, "CBaseGrenade", "m_flDetonateTime");
                    trackedNades.TryRemove(index, out _);
                    continue;
                }

                Vector currentVel = new Vector(nade.Velocity.X, nade.Velocity.Y, nade.Velocity.Z);
                Vector newVel = currentVel + pushForce;

                float speed = newVel.Length();
                if (speed > 2000f)
                    newVel *= 2000f / speed;

                trackedNades[index] = currentPos;
                nade.Teleport(null, null, newVel);
            }
        }

        private static Vector CalculatePushForce(CBaseCSGrenadeProjectile nade, int team)
        {
            if (nade.AbsOrigin == null) return Vector.Zero;
            Vector nadePos = nade.AbsOrigin;

            CCSPlayerController? closestEnemy = null;
            double minDistance = double.MaxValue;

            foreach (var enemy in SkillUtils.CachedPlayers)
            {
                if (enemy.TeamNum == team || !enemy.PawnIsAlive || enemy.Team == CsTeam.Spectator) 
                    continue;

                var pawn = enemy.PlayerPawn.Value;
                if (pawn?.IsValid == false || pawn?.AbsOrigin == null) continue;

                double dist = SkillUtils.GetDistance(nadePos, pawn.AbsOrigin);

                if (dist < 130f)
                    return Vector.Zero;

                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy == null) 
                return Vector.Zero;

            Vector targetPos = closestEnemy.PlayerPawn.Value!.AbsOrigin!;
            Vector direction = targetPos - nadePos;
            float length = direction.Length();

            if (length <= 0) 
                return Vector.Zero;

            return direction / length * 110f;
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            string name = entity.DesignerName;
            if (!name.EndsWith("_projectile") || name == "smokegrenade_projectile") 
                return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade == null || !grenade.IsValid) 
                return;

            // Find the thrower
            if (grenade.OwnerEntity?.Value == null || !grenade.OwnerEntity.Value.IsValid) 
                return;
            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn.Controller?.Value == null || !pawn.Controller.Value.IsValid) 
                return;
            var player = pawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) 
                return;

            if (grenade.AbsOrigin == null) 
                return;

            trackedNades.TryAdd(grenade.Index, new Vector(grenade.AbsOrigin.X, grenade.AbsOrigin.Y, grenade.AbsOrigin.Z));

            Server.NextFrame(() =>
            {
                if (grenade.IsValid)
                {
                    grenade.DetonateTime += 100f;
                    Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
                }
            });
        }
    }
}
