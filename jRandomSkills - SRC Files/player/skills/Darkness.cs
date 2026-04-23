using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.UserMessages;
using static jRandomSkills.jRandomSkills;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace jRandomSkills
{
    public class Darkness : ISkill
    {
        private const Skills skillName = Skills.Darkness;
        private static readonly ConcurrentDictionary<ulong, byte> playersInDark = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Fentanyl", "Do końca rundy, przyciemniasz ekran 1 wroga", "#575454");
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (playersInDark.ContainsKey(player.SteamID))
                        DisableSkill(player);
                    SkillUtils.CloseMenu(player);
                }
                playersInDark.Clear();
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

        private static void SetUpPostProcessing(CCSPlayerController player, bool turnOff = false)
        {
            if (player == null || !player.IsValid) return;
            ulong playerSteamID = player.SteamID;

            lock (setLock)
            {
                if (!turnOff)
                {
                    playersInDark.TryAdd(playerSteamID, 0);
                    ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 230, duration: 100, holdTime: 3000);

                    Timer? darkTimer = null;
                    darkTimer = Instance?.AddTimer(5f, () => {
                        if (!playersInDark.ContainsKey(playerSteamID))
                        {
                            darkTimer?.Kill();
                            return;
                        }

                        var target = Utilities.GetPlayerFromSteamId(playerSteamID);
                        if (target == null || !target.IsValid)
                        {
                            darkTimer?.Kill();
                            return;
                        }

                        if (target.PawnIsAlive)
                            ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 230, duration: 100, holdTime: 3000);
                    }, TimerFlags.STOP_ON_MAPCHANGE | TimerFlags.REPEAT);
                }
                else
                {
                    ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 0, duration: 200, holdTime: 0);
                    playersInDark.TryRemove(player.SteamID, out _);
                }
            }
        }

        private static void ApplyScreenColor(CCSPlayerController player, int r, int g, int b, int a, int duration, int holdTime, int flags = 1)
        {
            using var msg = UserMessage.FromPartialName("Fade");
            if (msg == null) return;
            int packageColor = (a << 24) | (b << 16) | (g << 8) | r;

            msg.SetInt("duration", duration);
            msg.SetInt("hold_time", holdTime);

            msg.SetInt("flags", flags);
            msg.SetInt("color", packageColor);

            msg.Send(player);
        }
    }
}