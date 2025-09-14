using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Darkness : ISkill
    {
        private const Skills skillName = Skills.Darkness;
        private static readonly float brightness = Config.GetValue<float>(skillName, "brightness");
        private static readonly Dictionary<CCSPlayerController, List<CPostProcessingVolume>> defaultPostProcessings = [];
        private static readonly List<CPostProcessingVolume> newPostProcessing = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            foreach (var player in defaultPostProcessings.Keys)
                DisableSkill(player);
            foreach (var postProcessing in newPostProcessing)
                if (postProcessing != null && postProcessing.IsValid)
                    postProcessing.AcceptInput("Kill");
            newPostProcessing.Clear();
            foreach (var player in Utilities.GetPlayers())
                SkillUtils.CloseMenu(player);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null) return;
            DisableSkill(player);
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if (!SkillUtils.HasMenu(player)) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();

                HashSet<(string, string)> menuItems = enemies.Select(e => (e.PlayerName, e.Index.ToString())).ToHashSet();
                SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                player.PrintToChat($" {ChatColors.Red}{Localization.GetTranslation("areareaper_used_info")}");
                return;
            }

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Team != player.Team && p.Index.ToString() == enemyId);

            if (enemy == null)
            {
                player.PrintToChat($" {ChatColors.Red}" + Localization.GetTranslation("selectplayerskill_incorrect_enemy_index"));
                return;
            }

            SetUpPostProcessing(enemy);
            playerInfo.SkillChance = 1;
            player.PrintToChat($" {ChatColors.Green}" + Localization.GetTranslation("darkness_player_info", enemy.PlayerName));
            enemy.PrintToChat($" {ChatColors.Red}" + Localization.GetTranslation("darkness_enemy_info"));
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                HashSet<(string, string)> menuItems = enemies.Select(e => (e.PlayerName, e.Index.ToString())).ToHashSet();
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                player.PrintToChat($" {ChatColors.Red}{Localization.GetTranslation("selectplayerskill_incorrect_enemy_index")}");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SetUpPostProcessing(player, true);
            SkillUtils.CloseMenu(player);
        }

        private static void SetUpPostProcessing(CCSPlayerController player, bool dontCreateNew = false)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) return;

            if (!defaultPostProcessings.TryGetValue(player, out _))
                defaultPostProcessings[player] = [];

            int i = 0;
            foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
            {
                if (postProcessingVolume == null || postProcessingVolume.Value == null)
                    return;

                if (dontCreateNew)
                {
                    if (i < defaultPostProcessings[player].Count)
                        postProcessingVolume.Raw = defaultPostProcessings[player][i].EntityHandle.Raw;
                }
                else
                {
                    defaultPostProcessings[player].Add(postProcessingVolume.Value);

                    var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
                    if (postProcessing == null) return;

                    postProcessing.ExposureControl = true;
                    postProcessing.MaxExposure = brightness;
                    postProcessing.MinExposure = brightness;

                    newPostProcessing.Add(postProcessing);
                    postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;
                }
                i++;
            }

            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
            if (dontCreateNew)
                defaultPostProcessings.Remove(player);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#575454", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float brightness = .05f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float Brightness { get; set; } = brightness;
        }
    }
}