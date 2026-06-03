using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;
using System.Collections.Concurrent;

namespace Supermoce
{
    public class Dash : ISkill
    {
        private const Skills skillName = Skills.Dash;

        private static readonly ConcurrentDictionary<ulong, PlayerDashState> PlayerStates = [];

        private class PlayerDashState
        {
            public PlayerButtons LastButtons { get; set; }
            public int AirJumpCount { get; set; }
        }

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                "Superman",
                "Naciśnięcie skoku będąc w powietrzu wykonuje DASH",
                "#42bbfc");
        }

        public static void NewRound()
        {
            PlayerStates.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            PlayerStates.TryAdd(player.SteamID, new PlayerDashState());
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            PlayerStates.TryRemove(player.SteamID, out _);
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill != skillName) continue;

                HandleDash(player);
            }
        }

        private static void HandleDash(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            if (!PlayerStates.TryGetValue(player.SteamID, out var state)) return;

            var currentButtons = player.Buttons;
            bool isOnGround = ((PlayerFlags)pawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND);

            if (isOnGround)
            {
                state.AirJumpCount = 0;
            }
            else if (!state.LastButtons.HasFlag(PlayerButtons.Jump) && 
                     currentButtons.HasFlag(PlayerButtons.Jump) && 
                     state.AirJumpCount < 1)
            {
                state.AirJumpCount++;

                float dirX = 0, dirY = 0;
                if (currentButtons.HasFlag(PlayerButtons.Forward))  dirY += 1;
                if (currentButtons.HasFlag(PlayerButtons.Back))     dirY -= 1;
                if (currentButtons.HasFlag(PlayerButtons.Moveleft)) dirX += 1;
                if (currentButtons.HasFlag(PlayerButtons.Moveright))dirX -= 1;

                if (dirX == 0 && dirY == 0) dirY = 1;

                float moveAngle = MathF.Atan2(dirX, dirY) * (180f / MathF.PI);
                QAngle dashAngles = new(0, pawn.EyeAngles.Y + moveAngle, 0);

                Vector vel = SkillUtils.GetForwardVector(dashAngles) * 500f;
                pawn.AbsVelocity.X = vel.X;
                pawn.AbsVelocity.Y = vel.Y;
                pawn.AbsVelocity.Z = vel.Z + 130f;
            }

            state.LastButtons = currentButtons;
        }
    }
}