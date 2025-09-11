using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Flash : ISkill
    {
        private const Skills skillName = Skills.Flash;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
            Instance.RegisterListener<OnTick>(UpdateSpeed);

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () => 
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn == null || !playerPawn.IsValid) continue;

                        playerPawn.VelocityModifier = 1f;
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill != skillName) continue;
                        EnableSkill(player);
                    }
                });
                
                return HookResult.Continue;
            });

            Instance.HookUserMessage(208, um =>
            {
                var soundevent = um.ReadUInt("soundevent_hash");
                var userIndex = um.ReadUInt("source_entity_index");

                if (userIndex == 0) return HookResult.Continue;
                if (!Instance.footstepSoundEvents.Contains(soundevent)) return HookResult.Continue;
                
                var player = Utilities.GetPlayers().FirstOrDefault(p => p.Pawn?.Value != null && p.Pawn.Value.IsValid && p.Pawn.Value.Index == userIndex);
                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;

                if (player.Buttons.HasFlag(PlayerButtons.Speed) || player.Buttons.HasFlag(PlayerButtons.Duck))
                {
                    um.Recipients.Clear();
                    return HookResult.Handled;
                }

                return HookResult.Continue;
            }, HookMode.Pre);
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

            playerPawn.VelocityModifier = newSpeed;
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("flash")}{ChatColors.Lime}: " + Localization.GetTranslation("flash_desc2", newSpeed), false);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            playerPawn.VelocityModifier = 1f;
        }

        private static void UpdateSpeed()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn != null && playerPawn.VelocityModifier != playerInfo?.SkillChance)
                    playerPawn.VelocityModifier = Math.Max((float)(playerInfo?.SkillChance ?? 1), 1);
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#A31912", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float chanceFrom = 1.3f, float chanceTo = 2.5f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
        }
    }
}