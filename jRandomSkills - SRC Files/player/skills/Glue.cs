using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Glue : ISkill
    {
        private const Skills skillName = Skills.Glue;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Semtex", "Twoje granaty przyklejają się do ścian", "#fff52e");
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            var name = entity.DesignerName;
            if (!name.EndsWith("_projectile"))
                return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;

            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;

            var player = pawn.Controller.Value.As<CCSPlayerController>();
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            grenade.Bounces = 555;
        }
    }
}