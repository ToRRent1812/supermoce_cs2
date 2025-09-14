using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Flash : ISkill
    {
        private const Skills skillName = Skills.Flash;
        public static readonly Dictionary<ulong, int> jumpedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public static void NewRound()
        {
            jumpedPlayers.Clear();
        }

        public static void PlayerMakeSound(UserMessage um)
        {
            var soundevent = um.ReadUInt("soundevent_hash");
            var userIndex = um.ReadUInt("source_entity_index");

            if (userIndex == 0) return;
            if (!Instance.footstepSoundEvents.Contains(soundevent)) return;

            var player = Utilities.GetPlayers().FirstOrDefault(p => p.Pawn?.Value != null && p.Pawn.Value.IsValid && p.Pawn.Value.Index == userIndex);
            if (!Instance.IsPlayerValid(player)) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (player!.Buttons.HasFlag(PlayerButtons.Speed) || player.Buttons.HasFlag(PlayerButtons.Duck))
                um.Recipients.Clear();
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            if (!jumpedPlayers.TryGetValue(player.SteamID, out _)) return;
            jumpedPlayers[player.SteamID] = Server.TickCount + 20;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerPawn == null || playerInfo == null) return;

            var skillConfig = Config.LoadedConfig.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString());
            if (skillConfig == null) return;

            float newSpeed = (float)Instance.Random.NextDouble() * (Config.GetValue<float>(skillName, "ChanceTo") - Config.GetValue<float>(skillName, "ChanceFrom")) + Config.GetValue<float>(skillName, "ChanceFrom");
            newSpeed = (float)Math.Round(newSpeed, 2);
            playerInfo.SkillChance = newSpeed;
            playerInfo.RandomPercentage = ((int)(newSpeed * 100)).ToString() + "%";

            jumpedPlayers.TryAdd(player.SteamID, 0);
            playerPawn.VelocityModifier = newSpeed;
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("flash")}{ChatColors.Lime}: " + Localization.GetTranslation("flash_desc2", newSpeed), false);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            playerPawn.VelocityModifier = 1;
            jumpedPlayers.Remove(player.SteamID);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier == 0) continue;

                var buttons = player.Buttons;
                float newVelocity = Math.Max((float)(playerInfo?.SkillChance ?? 1), 1);
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = newVelocity;

                if (jumpedPlayers.TryGetValue(player.SteamID, out var time) && time > Server.TickCount)
                    continue;

                if (!((PlayerFlags)player.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
                    playerPawn.AbsVelocity.Z = Math.Min(playerPawn.AbsVelocity.Z, 10);
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#A31912", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float chanceFrom = 1.3f, float chanceTo = 2.5f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
        }
    }
}