using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Darkness : ISkill
    {
        private const Skills skillName = Skills.Darkness;
        private static readonly ConcurrentDictionary<CCSPlayerController, List<CPostProcessingVolume>> defaultPostProcessings = [];
        private static readonly ConcurrentBag<CPostProcessingVolume> newPostProcessing = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Fentanyl", "Do końca rundy, przyciemniasz ekran 1 wroga", "#575454");
        }

        public static void NewRound()
        {
            lock (setLock)
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
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo == null || playerInfo.Skill != skillName) continue;
                var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();

                ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => (e.PlayerName, e.Index.ToString()))];
                SkillUtils.UpdateMenu(player, menuItems);
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (playerInfo.SkillChance == 1)
            {
                player.PrintToChat($" {ChatColors.Red}Twoja moc została już wykorzystana.");
                return;
            }

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Team != player.Team && p.Index.ToString() == enemyId);

            if (enemy == null)
            {
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
                return;
            }

            SetUpPostProcessing(enemy);
            playerInfo.SkillChance = 1;
            player.PrintToChat($" {ChatColors.Green}Ciemność opanowała {enemy.PlayerName}.");
            enemy.PrintToChat($" {ChatColors.Red}Wróg zgasił Ci światło.");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator && p.Team != CsTeam.None).ToArray();
            if (enemies.Length > 0)
            {
                ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => (e.PlayerName, e.Index.ToString()))];
                SkillUtils.CreateMenu(player, menuItems);
            }
            else
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono gracza o takim ID.");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            lock (setLock)
            {
                SetUpPostProcessing(player, true);
                SkillUtils.CloseMenu(player);
            }
        }

        private static void SetUpPostProcessing(CCSPlayerController player, bool dontCreateNew = false)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) return;

            lock (setLock)
            {
                if (!defaultPostProcessings.ContainsKey(player))
                    defaultPostProcessings.TryAdd(player, []);

                int i = 0;
                foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
                {
                    if (postProcessingVolume == null || postProcessingVolume.Value == null)
                        return;

                    if (dontCreateNew)
                    {
                        if (defaultPostProcessings.TryGetValue(player, out var defaultList) && i < defaultList.Count)
                            postProcessingVolume.Raw = defaultList[i].EntityHandle.Raw;
                    }
                    else
                    {
                        if (defaultPostProcessings.TryGetValue(player, out var defaultList))
                            defaultList.Add(postProcessingVolume.Value);

                        var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
                        if (postProcessing == null) return;

                        postProcessing.ExposureControl = true;
                        postProcessing.MaxExposure = 0.06f;
                        postProcessing.MinExposure = 0.03f;

                        newPostProcessing.Add(postProcessing);
                        postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;
                    }
                    i++;
                }

                Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
                if (dontCreateNew)
                    defaultPostProcessings.TryRemove(player, out _);
            }
        }
    }
}