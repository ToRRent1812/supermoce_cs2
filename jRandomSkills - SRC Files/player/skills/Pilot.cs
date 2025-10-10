using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Pilot : ISkill
    {
        private const Skills skillName = Skills.Pilot;
        private static readonly ConcurrentDictionary<ulong, Pilot_PlayerInfo> PlayerPilotInfo = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Pilot", "Skocz i trzymaj klawisz rozbrajania, by latać", "#1466F5");
        }

        public static void NewRound()
        {
            PlayerPilotInfo.Clear();
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    HandlePilot(player);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            PlayerPilotInfo.TryAdd(player.SteamID, new Pilot_PlayerInfo
            {
                SteamID = player.SteamID,
                Fuel = 150f,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            PlayerPilotInfo.TryRemove(player.SteamID, out _);
        }

        private static void HandlePilot(CCSPlayerController player)
        {
            var buttons = player.Buttons;
            if (PlayerPilotInfo.TryGetValue(player.SteamID, out var pilotInfo))
            {
                pilotInfo.Fuel = Math.Min(Math.Max(0, pilotInfo.Fuel - (buttons.HasFlag(PlayerButtons.Use) ? 0.64f : -0.1f)), 150f);
                if (buttons.HasFlag(PlayerButtons.Use))
                    if (pilotInfo.Fuel > 0 && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && !player.PlayerPawn.Value.IsDefusing)
                        ApplyPilotEffect(player);
                UpdateHUD(player, pilotInfo);
            }
        }

        private static void UpdateHUD(CCSPlayerController player, Pilot_PlayerInfo pilotInfo)
        {
            var buttons = player.Buttons;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string fuelColor = GetFuelColor(pilotInfo.Fuel);
            string infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string skillLine = $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillData.Description}</font> <br>";
            string remainingLine = $"<font class='fontSize-m' color='#ffffff'>Paliwo:</font> <font color='{fuelColor}'>{pilotInfo.Fuel/150f*100:F0}%</font> <br>";

            var hudContent = infoLine + skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        private static string GetFuelColor(float fuelPercentage)
        {
            if (fuelPercentage > (150f/2f)) return "#00FF00";
            if (fuelPercentage > (150f/4f)) return "#FFFF00";
            return "#FF0000";
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
            public ulong SteamID { get; set; }
            public float Fuel { get; set; }
        }
    }
}