using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace jRandomSkills
{
    public class ExplosiveShot : ISkill
    {
        private const Skills skillName = Skills.ExplosiveShot;

        private static readonly QAngle angle = new(5, 10, -4);
        private static int lastTick = 0;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Wybuchowy strzał", "Szansa na wystrzelenie pocisku wybuchowego", "#9c0000");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            int randomValue = Instance?.Random?.Next(5,11) * 3 ?? 15; //15-30%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }

        private static void SpawnExplosion(Vector vector)
        {
            lastTick = Server.TickCount;
            SkillUtils.CreateHEGrenadeProjectile(vector, angle, new Vector(0, 0, 0), 0);
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (entity.DesignerName != "hegrenade_projectile") return;

            var heProjectile = entity.As<CBaseCSGrenadeProjectile>();
            if (heProjectile == null || !heProjectile.IsValid || heProjectile.AbsRotation == null) return;

            Server.NextFrame(() =>
            {
                if (heProjectile == null || !heProjectile.IsValid) return;
                if (!(NearlyEquals(angle.X, heProjectile.AbsRotation.X) && NearlyEquals(angle.Y, heProjectile.AbsRotation.Y) && NearlyEquals(angle.Z, heProjectile.AbsRotation.Z)))
                    return;

                heProjectile.TicksAtZeroVelocity = 100;
                heProjectile.TeamNum = (byte)CsTeam.None;
                heProjectile.Damage = 55f;
                heProjectile.DmgRadius = 260f;
                heProjectile.DetonateTime = 0;
            });
        }

        private static bool NearlyEquals(float a, float b, float epsilon = 0.001f) => Math.Abs(a -b) < epsilon;

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (lastTick == Server.TickCount) return HookResult.Continue;

            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);

            if (attackerPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null || playerInfo.Skill != skillName) return HookResult.Continue;
            if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                SpawnExplosion(info.DamagePosition);

            return HookResult.Continue;
        }
    }
}