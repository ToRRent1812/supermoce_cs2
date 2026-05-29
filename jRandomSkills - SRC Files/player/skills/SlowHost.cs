using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class SlowHost : ISkill
    {
        private const Skills skillName = Skills.SlowHost;
        private static bool skillExists = false;
        private static readonly ConcurrentDictionary<ulong, int> affectedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ciężarne hosty", "Póki jesteś żywy, chodzenie z hostem jest znacznie wolniejsze.", "#fd4371", 1, 2);
        }

        public static void NewRound()
        {
            skillExists = false;
            affectedPlayers.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid) return;
            var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(player.SteamID, out var skillPlayer) ? skillPlayer : null;
            if (playerInfo == null || playerInfo?.Skill != skillName) return;
            skillExists = true;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid) return;
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid)
                {
                    var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(p.SteamID, out var skillPlayer) ? skillPlayer : null;
                    if (p.PawnIsAlive && playerInfo?.Skill == skillName) return;
                }
            }
            skillExists = false;
        }

        public static void HostageFollows(EventHostageFollows @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = Instance?.SkillPlayerDict?.TryGetValue(player.SteamID, out var skillPlayer) ? skillPlayer : null;
            if (playerInfo == null) return;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            if(skillExists)
            {
                affectedPlayers.TryAdd(player.SteamID, 0);
                playerPawn.VelocityModifier = 0.6f;
            } 
        }

        public static void OnTick()
        {
            if (!skillExists) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if(!affectedPlayers.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier == 0) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 0.6f;
            }
        }
    }
}