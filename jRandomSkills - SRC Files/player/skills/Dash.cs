using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Dash : ISkill
    {
        private const Skills skillName = Skills.Dash;
        private static readonly PlayerFlags[] LF = new PlayerFlags[64];
        private static readonly int?[] J = new int?[64];
        private static readonly PlayerButtons[] LB = new PlayerButtons[64];

        private static readonly float jumpVelocity = Config.GetValue<float>(skillName, "jumpVelocity");
        private static readonly float pushVelocity = Config.GetValue<float>(skillName, "pushVelocity");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) return;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
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

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerPawn == null || playerInfo == null) return;

            if ((LF[player.Slot] & PlayerFlags.FL_ONGROUND) != 0 && (flags & PlayerFlags.FL_ONGROUND) == 0 && (LB[player.Slot] & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0)
            {
            }
            else if ((flags & PlayerFlags.FL_ONGROUND) != 0)
            {
                J[player.Slot] = 0;
            }
            else if ((LB[player.Slot] & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0 && J[player.Slot] < 1)
            {
                J[player.Slot]++;

                                float moveX = 0;
                float moveY = 0;

                PlayerButtons playerButtons = player.Buttons;
                if (playerButtons.HasFlag(PlayerButtons.Forward))
                    moveY += 1;
                if (playerButtons.HasFlag(PlayerButtons.Back))
                    moveY -= 1;
                if (playerButtons.HasFlag(PlayerButtons.Moveleft))
                    moveX += 1;
                if (playerButtons.HasFlag(PlayerButtons.Moveright))
                    moveX -= 1;

                if (moveX == 0 && moveY == 0)
                    moveY = 1;

                float moveAngle = MathF.Atan2(moveX, moveY) * (180f / MathF.PI);
                QAngle dashAngles = new(0, playerPawn.EyeAngles.Y + moveAngle, 0);

                Vector newVelocity = SkillUtils.GetForwardVector(dashAngles) * pushVelocity;
                newVelocity.Z = playerPawn.AbsVelocity.Z + jumpVelocity;

                playerPawn.AbsVelocity.X = newVelocity.X;
                playerPawn.AbsVelocity.Y = newVelocity.Y;
                playerPawn.AbsVelocity.Z = newVelocity.Z;
            }

            LF[player.Slot] = flags;
            LB[player.Slot] = buttons;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#42bbfc", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float jumpVelocity = 150f, float pushVelocity = 600f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float JumpVelocity { get; set; } = jumpVelocity;
            public float PushVelocity { get; set; } = pushVelocity;
        }
    }
}