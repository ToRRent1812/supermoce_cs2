using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FastEscape : ISkill
    {
        private const Skills skillName = Skills.FastEscape;
        private static readonly ConcurrentDictionary<ulong, int> affectedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Szybka pomoc", "Z hostem poruszasz się szybciej", "#1279ff", 2, 2);
        }

        public static void NewRound()
        {
            affectedPlayers.Clear();
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            playerPawn.VelocityModifier = 1f;
            affectedPlayers.TryRemove(player.SteamID, out _);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!affectedPlayers.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier != 2.25f) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 2.25f;
            }
        }

        public static void HostageFollows(EventHostageFollows @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null || playerInfo.Skill != skillName) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            affectedPlayers.TryAdd(player.SteamID, 0);
        }
    }
}