using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class PawelJumper : ISkill
    {
        private const Skills skillName = Skills.PawelJumper;
        private static readonly int extraJumpsMin = Config.GetValue<int>(skillName, "extraJumpsMin");
        private static readonly int extraJumpsMax = Config.GetValue<int>(skillName, "extraJumpsMax");

        private static readonly PlayerFlags[] LF = new PlayerFlags[64];
        private static readonly int?[] J = new int?[64];
        private static readonly PlayerButtons[] LB = new PlayerButtons[64];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
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

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerPawn == null || playerInfo == null) return;

            var skillConfig = Config.LoadedConfig.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString());
            if (skillConfig == null) return;

            float extraJumps = (float)Instance.Random.Next(extraJumpsMin, extraJumpsMax + 1);
            playerInfo.SkillChance = extraJumps;
            playerInfo.RandomPercentage = $"+{extraJumps}";
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{player.GetTranslation("paweljumper")}{ChatColors.Lime}: " + player.GetTranslation("paweljumper_desc2", extraJumps), false);
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
                //J[player.Slot] ++;
            }
            else if ((flags & PlayerFlags.FL_ONGROUND) != 0)
            {
                J[player.Slot] = 0;
            }
            else if ((LB[player.Slot] & PlayerButtons.Jump) == 0 && (buttons & PlayerButtons.Jump) != 0 && J[player.Slot] < playerInfo.SkillChance)
            {
                J[player.Slot]++;
                playerPawn.AbsVelocity.Z = 300;
            }

            LF[player.Slot] = flags;
            LB[player.Slot] = buttons;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FFA500", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int extraJumpsMin = 1, int extraJumpsMax = 3) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int ExtraJumpsMin { get; set; } = extraJumpsMin;
            public int ExtraJumpsMax { get; set; } = extraJumpsMax;
        }
    }
}