using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;

namespace Supermoce
{
    public class PawelJumper : ISkill
    {
        private const Skills skillName = Skills.PawelJumper;
        private static readonly PlayerFlags[] LF = new PlayerFlags[64];
        private static readonly int?[] J = new int?[64];
        private static readonly PlayerButtons[] LB = new PlayerButtons[64];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Paweł Jumper", 
            "Nieskończone skoki na spacji", 
            "#FFA500");
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (player.PlayerPawn?.Value?.Health <= 0) continue;
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName)
                    GiveAdditionalJump(player);
            }
        }

        private static void GiveAdditionalJump(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;

            var flags = (PlayerFlags)playerPawn.Flags;
            var buttons = player.Buttons;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerPawn == null || playerInfo == null) return;

            if ((LB[player.Slot] & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0)
            {
                playerPawn.AbsVelocity.Z = 280;
            }

            LF[player.Slot] = flags;
            LB[player.Slot] = buttons;
        }
    }
}