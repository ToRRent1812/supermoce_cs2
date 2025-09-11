using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Spectator : ISkill
    {
        private const Skills skillName = Skills.Spectator;
        private static bool blocked = false;
        private static readonly float distance = Config.GetValue<float>(skillName, "distance");
        private static readonly Dictionary<ulong, (uint, CDynamicProp, CCSPlayerPawn)> cameras = [];

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
                {
                    if (camera.Value.Item2.IsValid) camera.Value.Item2.Remove();
                }
                cameras.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                blocked = false;
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventItemPickup>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);

                if (playerInfo?.Skill != skillName) return HookResult.Continue;

                var pawn = player!.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) return HookResult.Continue;
                if (cameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.Item1 != pawn.CameraServices.ViewEntity.Raw)
                    BlockWeapon(player, true);
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
                    var pawn = cameraInfo.Item3;
                    if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    {
                        ChangeCamera(player, true);
                        continue;
                    }
                    if (pawn.AbsOrigin == null) continue;
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
            if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) return;
            if (cameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.Item2.IsValid)
            {
                orginalCameraRaw = cameraInfo.Item1;
                cameraInfo.Item2.Remove();
                newCameraRaw = CreateCamera(player);
            }
            else
            {
                orginalCameraRaw = pawn.CameraServices.ViewEntity.Raw;
                newCameraRaw = CreateCamera(player);
            }

            if (newCameraRaw == 0) return;

            bool defaultCam = forceToDefault || (pawn.CameraServices.ViewEntity.Raw != orginalCameraRaw);
            pawn.CameraServices.ViewEntity.Raw = defaultCam ? orginalCameraRaw : newCameraRaw;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
            BlockWeapon(player, !defaultCam);
        }

        private static uint CreateCamera(CCSPlayerController player)
        {
            var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (camera == null || !camera.IsValid) return 0;

            var enemies = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != player.Team).ToList();
            if (enemies.Count == 0)
                return 0;
            var enemy = enemies[Instance.Random.Next(enemies.Count)];

            var pawn = enemy.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) return 0;
            camera.Render = Color.FromArgb(0, 255, 255, 255);
            camera.Teleport(pawn!.AbsOrigin, pawn.EyeAngles);
            camera.DispatchSpawn();
            if (cameras.TryGetValue(player.SteamID, out var cameraInfo))
                cameras[player.SteamID] = (cameraInfo.Item1, camera, pawn);
            else
                cameras[player.SteamID] = (pawn.CameraServices.ViewEntity.Raw, camera, pawn);
            return camera.EntityHandle.Raw;
        }

        private static void BlockWeapon(CCSPlayerController player, bool block)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
                if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                {
                    weapon.Value.NextPrimaryAttackTick = block ? int.MaxValue : Server.TickCount;
                    weapon.Value.NextSecondaryAttackTick = block ? int.MaxValue : Server.TickCount;

                    Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                    Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
                }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#42f5da", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float distance = 125f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float Distance { get; set; } = distance;
        }
    }
}