using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;

namespace Supermoce
{
    public class Pilot : ISkill
    {
        private const Skills skillName = Skills.Pilot;
        private static readonly ConcurrentDictionary<int, Pilot_PlayerInfo> PlayerPilotInfo = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Pilot", 
            "Skocz i trzymaj klawisz rozbrajania, by latać", 
            "#1466F5");
        }

        public static void NewRound()
        {
            PlayerPilotInfo.Clear();
        }

        public static void OnTick()
        {
            if (SkillUtils.IsFreezetime()) return;
            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName)
                    HandlePilot(player);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            PlayerPilotInfo.TryAdd(player.Slot, new Pilot_PlayerInfo
            {
                Slot = player.Slot,
                Fuel = 100f,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            PlayerPilotInfo.TryRemove(player.Slot, out _);
        }

        private static void HandlePilot(CCSPlayerController player)
        {
            var buttons = player.Buttons;
            if (PlayerPilotInfo.TryGetValue(player.Slot, out var pilotInfo))
            {
                pilotInfo.Fuel = Math.Min(Math.Max(0, pilotInfo.Fuel - (buttons.HasFlag(PlayerButtons.Use) ? 0.5f : -0.1f)), 100f);
                if (buttons.HasFlag(PlayerButtons.Use))
                    if (pilotInfo.Fuel > 0 && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && !player.PlayerPawn.Value.IsDefusing)
                        ApplyPilotEffect(player);
                UpdateHUD(player, pilotInfo);
            }
        }

        private static void UpdateHUD(CCSPlayerController player, Pilot_PlayerInfo pilotInfo)
        {
            SkillHudPanel.ShowStatus(player, skillName, $"Paliwo: {pilotInfo.Fuel:F0}%");
        }


        private static void ApplyPilotEffect(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            
            QAngle eye_angle = playerPawn.EyeAngles;
            double pitch = Math.PI / 180 * eye_angle.X;
            double yaw = Math.PI / 180 * eye_angle.Y;
            Vector eye_vector = new((float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)(Math.Sin(yaw) * Math.Cos(pitch)), (float)(-Math.Sin(pitch)));

            Vector currentVelocity = playerPawn.AbsVelocity;

            Vector jetpackVelocity = new(
                eye_vector.X * 7.0f,
                eye_vector.Y * 7.0f,
                0.80f * 15.0f
            );

            float newVelocityX = currentVelocity.X + jetpackVelocity.X;
            float newVelocityY = currentVelocity.Y + jetpackVelocity.Y;
            float newVelocityZ = currentVelocity.Z + jetpackVelocity.Z;

            playerPawn.AbsVelocity.X = newVelocityX;
            playerPawn.AbsVelocity.Y = newVelocityY;
            playerPawn.AbsVelocity.Z = newVelocityZ;
        }


        public class Pilot_PlayerInfo
        {
            public int Slot { get; set; }
            public float Fuel { get; set; }
        }
    }
}