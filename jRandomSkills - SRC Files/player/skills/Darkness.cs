using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.UserMessages;
using static jRandomSkills.jRandomSkills;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace jRandomSkills
{
    public class Darkness : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Darkness;
        private static readonly ConcurrentDictionary<ulong, byte> playersInDark = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Fentanyl", 
            "Do końca rundy, przyciemniasz ekran 1 wroga", 
            "#575454");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
            List<CCSPlayerController> allPlayers;
            lock (setLock)
            {
                allPlayers = Utilities.GetPlayers().ToList();
                playersInDark.Clear();
            }

            foreach (var player in allPlayers)
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 0, duration: 200, holdTime: 0);
            }
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null) return;
            DisableSkill(player);
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy, alreadyUsedMsg: "Twoja moc została już wykorzystana."))
                return;

            SetUpPostProcessing(enemy!);
            if (playerInfo != null) playerInfo.SkillChance = 1;
            SkillUtils.PrintToChat(enemy!, $" Wróg zgasił Ci światło");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            lock (setLock)
            {
                SetUpPostProcessing(player, true);
            }
        }

        private static void SetUpPostProcessing(CCSPlayerController player, bool turnOff = false)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ulong playerSteamID = player.SteamID;

            lock (setLock)
            {
                if (!turnOff)
                {
                    playersInDark.TryAdd(playerSteamID, 0);
                    ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 235, duration: 100, holdTime: 3000);

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
                            ApplyScreenColor(player, r: 0, g: 0, b: 0, a: 235, duration: 100, holdTime: 3000);
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