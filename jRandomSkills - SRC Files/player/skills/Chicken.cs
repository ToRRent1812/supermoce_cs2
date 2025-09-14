using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;
using jRandomSkills.src.utils;
using CounterStrikeSharp.API.Core.Attributes;

namespace jRandomSkills
{
    public class Chicken : ISkill
    {
        private const Skills skillName = Skills.Chicken;
        private static readonly string[] disabledWeapons =
        [
            "weapon_ak47", "weapon_m4a4", "weapon_m4a1", "weapon_m4a1_silencer",
            "weapon_famas", "weapon_galilar", "weapon_aug", "weapon_sg553", 
            "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7",
            "weapon_ump45", "weapon_p90", "weapon_mp5sd", "weapon_ssg08",
            "weapon_awp", "weapon_scar20", "weapon_g3sg1", "weapon_nova",
            "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249",
            "weapon_negev"
        ];
        private static readonly Dictionary<CCSPlayerController, CBaseModelEntity> chickens = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            foreach (var player in Utilities.GetPlayers())
                SetWeaponAttack(player, false);
        }

        public static void WeaponPickup(EventItemPickup @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;
            SetWeaponAttack(player, true);
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                if (chickens.TryGetValue(player, out var chicken))
                    if (chicken != null && chicken.IsValid)
                        info.TransmitEntities.Remove(chicken.Index);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null && playerPawn.IsValid)
            {
                Event.enableTransmit = true;

                playerPawn.Health = 50;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");

                SkillUtils.ChangePlayerScale(player, .25f);

                playerPawn.Render = Color.FromArgb(0, 255, 255, 255);
                playerPawn.ShadowStrength = 0.0f;
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");

                SetWeaponAttack(player, true);
                CreateChicken(player);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn != null)
            {
                playerPawn.Health += 50;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");

                SkillUtils.ChangePlayerScale(player, 1);

                playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
                playerPawn.ShadowStrength = 1.0f;
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");

                SetWeaponAttack(player, false);
            }

            if (chickens.TryGetValue(player, out var chicken))
            {
                if (chicken != null && chicken.IsValid)
                    chicken.AcceptInput("Kill");
                chickens.Remove(player);
            }
        }

        private static void SetWeaponAttack(CCSPlayerController player, bool disableWeapon)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player?.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null || pawn.WeaponServices.MyWeapons == null) return;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
                if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                    if (disabledWeapons.Contains(weapon.Value.DesignerName))
                    {
                        weapon.Value.NextPrimaryAttackTick = disableWeapon ? int.MaxValue : Server.TickCount;
                        weapon.Value.NextSecondaryAttackTick = disableWeapon ? int.MaxValue : Server.TickCount;

                        Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                        Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
                    }
        }

        private static void CreateChicken(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;
            var chickenModel = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (chickenModel == null)
                return;
            Vector pos = new(0, 0, 0);

            chickenModel.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(chickenModel.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            chickenModel.SetModel("models/chicken/chicken.vmdl");
            chickenModel.Render = Color.FromArgb(255, 255, 255, 255);
            chickenModel.Teleport(pos, playerPawn.AbsRotation, null);
            chickenModel.DispatchSpawn();
            chickenModel.AcceptInput("InitializeSpawnFromWorld", playerPawn, playerPawn, "");
            Utilities.SetStateChanged(chickenModel, "CBaseEntity", "m_CBodyComponent");
            Instance.AddTimer(1f, () => chickens.TryAdd(player, chickenModel));
        }

        public static void OnTick()
        {
            foreach (var valuePair in chickens)
            {
                var player = valuePair.Key;
                var chicken = valuePair.Value;
                if (player == null || !player.IsValid) continue;
                if (chicken == null || !chicken.IsValid) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || chicken.AbsOrigin == null) continue;

                float X = (float)Math.Round(pawn.AbsOrigin.X, 2);
                float Y = (float)Math.Round(pawn.AbsOrigin.Y, 2);
                float Z = (float)Math.Round(pawn.AbsOrigin.Z, 2);
                Vector pos = new(X, Y, Z);
                if (chicken.AbsOrigin.X != pos.X || chicken.AbsOrigin.Y != pos.Y || chicken.AbsOrigin.Z != pos.Z)
                    chicken.Teleport(pos, pawn.AbsRotation, null);
                UpdateHUD(player);
            }
        }

        private static void UpdateHUD(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn.WeaponServices == null || pawn.WeaponServices.ActiveWeapon == null || !pawn.WeaponServices.ActiveWeapon.IsValid || pawn.WeaponServices.ActiveWeapon.Value == null || !pawn.WeaponServices.ActiveWeapon.Value.IsValid) return;
            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            var weapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (weapon == null || !disabledWeapons.Contains(weapon.DesignerName)) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("disabled_weapon")}</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FF8B42", CsTeam onlyTeam = CsTeam.Terrorist, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}