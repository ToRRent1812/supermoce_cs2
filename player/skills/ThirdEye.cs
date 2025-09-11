using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ThirdEye : ISkill
    {
        private const Skills skillName = Skills.ThirdEye;
        private static bool blocked = false;
        private static readonly float distance = Config.GetValue<float>(skillName, "distance");
        private static readonly Dictionary<ulong, (uint, CDynamicProp)> cameras = [];
        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                blocked = true;
                foreach (var player in Utilities.GetPlayers())
                    if (cameras.TryGetValue(player.SteamID, out _))
                        DisableSkill(player);

                foreach (var camera in cameras)
                    camera.Value.Item2.Remove();
                cameras.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                blocked = false;
                return HookResult.Continue;
            });

            Instance.RegisterListener<Listeners.OnTick>(OnTick);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null || blocked) return;
            ChangeCamera(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            ChangeCamera(player, true);
        }

        private static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
                if (cameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.Item2.IsValid)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) continue;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        ChangeCamera(player, true);
                        continue;
                    }
                    var pos = pawn.AbsOrigin! - SkillUtils.GetForwardVector(pawn.EyeAngles) * distance;
                    pos.Z += pawn.ViewOffset.Z;
                    cameraInfo.Item2.Teleport(pos, pawn.V_angle);
                }
        }

        private static void ChangeCamera(CCSPlayerController player, bool forceToDefault = false)
        {
            uint orginalCameraRaw;
            uint newCameraRaw;
            var pawn = player.PlayerPawn.Value;
            if (cameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.Item2.IsValid)
            {
                orginalCameraRaw = cameraInfo.Item1;
                newCameraRaw = cameraInfo.Item2.EntityHandle.Raw;
            } else
            {
                orginalCameraRaw = pawn!.CameraServices!.ViewEntity.Raw;
                newCameraRaw = CreateCamera(player);
            }

            if (newCameraRaw == 0)
                return;

            if (forceToDefault)
                pawn!.CameraServices!.ViewEntity.Raw = orginalCameraRaw;
            else
                pawn!.CameraServices!.ViewEntity.Raw =
                                pawn.CameraServices!.ViewEntity!.Raw == orginalCameraRaw
                                ? newCameraRaw
                                : orginalCameraRaw;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
        }

        private static uint CreateCamera(CCSPlayerController player)
        {
            var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (camera == null || !camera.IsValid) return 0;

            var pawn = player.PlayerPawn.Value;
            camera.Render = Color.FromArgb(0, 255, 255, 255);
            camera.Teleport(pawn!.AbsOrigin, pawn.EyeAngles);
            camera.DispatchSpawn();
            cameras[player.SteamID] = (pawn.CameraServices!.ViewEntity.Raw, camera);
            return camera.EntityHandle.Raw;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#1b04cc", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float distance = 120f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float Distance { get; set; } = distance;
        }
    }
}