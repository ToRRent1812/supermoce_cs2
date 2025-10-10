using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
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
                heProjectile.Damage = 35f;
                heProjectile.DmgRadius = 240f;
                heProjectile.DetonateTime = 0;
            });
        }

        private static bool NearlyEquals(float a, float b, float epsilon = 0.001f) => Math.Abs(a -b) < epsilon;

        public static void OnTakeDamage(DynamicHook h)
        {
            if (lastTick == Server.TickCount) return;

            CEntityInstance param = h.GetParam<CEntityInstance>(0);
            CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
                return;

            CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
            if (attackerPawn.DesignerName != "player")
                return;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null)
                return;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null || playerInfo.Skill != skillName) return;

            if (Instance?.Random.NextDouble() <= playerInfo.SkillChance)
                SpawnExplosion(param2.DamagePosition);
        }
    }
}