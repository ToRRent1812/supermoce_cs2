using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class MrBlysk : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.MrBlysk;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Mr. Błysk",
                "Możesz na żądanie upuścić flash na swoje nogi",
                "#FFDDFF",
                minCooldown: 15,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
                ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            Vector pos = new(pawn.AbsOrigin?.X ?? 0, pawn.AbsOrigin?.Y ?? 0, pawn.AbsOrigin?.Z ?? 0);
            SkillUtils.CreateFlashGrenadeProjectile(pos, QAngle.Zero, new Vector(0, 0, 0), player.TeamNum);
        }
    }
}
