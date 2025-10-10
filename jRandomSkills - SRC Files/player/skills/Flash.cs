using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Flash : ISkill
    {
        private const Skills skillName = Skills.Flash;
        public static readonly ConcurrentDictionary<ulong, int> jumpedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Struś", "Szybko biegasz", "#dd1ad3");
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
            if (Instance?.footstepSoundEvents.Contains(soundevent) == false) return;

            var player = Utilities.GetPlayers().FirstOrDefault(p => p.Pawn?.Value != null && p.Pawn.Value.IsValid && p.Pawn.Value.Index == userIndex);
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (player!.Buttons.HasFlag(PlayerButtons.Speed) || player.Buttons.HasFlag(PlayerButtons.Duck))
                um.Recipients.Clear();
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            if (!jumpedPlayers.TryGetValue(player.SteamID, out _)) return;
            jumpedPlayers.AddOrUpdate(player.SteamID, Server.TickCount + 20, (k, v) => Server.TickCount + 20);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerPawn == null || playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(13,26) * 10 ?? 130; //130-250%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";

            jumpedPlayers.TryAdd(player.SteamID, 0);
            playerPawn.VelocityModifier = playerInfo.SkillChance ?? 1f;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;
            playerPawn.VelocityModifier = 1f;
            jumpedPlayers.TryRemove(player.SteamID, out _);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (Instance?.IsPlayerValid(player) == false) continue;

                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
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
    }
}