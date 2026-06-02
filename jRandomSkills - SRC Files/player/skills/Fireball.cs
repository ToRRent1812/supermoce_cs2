using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Fireball : ISkill
    {
        private const Skills skillName = Skills.Fireball;
        private static readonly QAngle explosionAngle = new(77, 11, -22);

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Trotylov", 
            "Twoje mołotowy wybuchają", 
            "#e25d2d", 
            teamnum:1);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Molotov);
        }

        public static void MolotovDetonate(EventMolotovDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
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
                heProjectile.Damage = 150f;
                heProjectile.DmgRadius = 600f;
                heProjectile.DetonateTime = 0;
            });
        }

        private static bool NearlyEquals(float a, float b, float epsilon = 0.001f) => Math.Abs(a - b) < epsilon;
    }
}
