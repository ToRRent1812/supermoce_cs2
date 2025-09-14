using System.Collections.Immutable;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Ghost : ISkill
    {
        private const Skills skillName = Skills.Ghost;
        private static readonly string[] disabledWeapons =
        [
            "weapon_deagle", "weapon_revolver", "weapon_glock", "weapon_usp_silencer",
            "weapon_cz75a", "weapon_fiveseven", "weapon_p250", "weapon_tec9",
            "weapon_elite", "weapon_hkp2000", "weapon_ak47", "weapon_m4a1",
            "weapon_m4a4", "weapon_m4a1_silencer", "weapon_famas", "weapon_galilar",
            "weapon_aug", "weapon_sg553", "weapon_mp9", "weapon_mac10",
            "weapon_bizon", "weapon_mp7", "weapon_ump45", "weapon_p90",
            "weapon_mp5sd", "weapon_ssg08", "weapon_awp", "weapon_scar20",
            "weapon_g3sg1", "weapon_nova", "weapon_xm1014", "weapon_mag7",
            "weapon_sawedoff", "weapon_m249", "weapon_negev"
        ];
        private static readonly Dictionary<ulong, List<uint>> invisibleEntities = [];

        public static void LoadSkill()
        {
            if (Config.LoadedConfig.SkillsInfo.FirstOrDefault(s => s.Name == skillName.ToString())?.Active != true)
                return;

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
            if (!Instance.IsPlayerValid(player)) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);

            if (playerInfo?.Skill != skillName) return;
            SetWeaponVisibility(player!, false);
            SetWeaponAttack(player!, true);
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = @event.Userid;
            if (!Instance.IsPlayerValid(player)) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);

            if (playerInfo?.Skill != skillName) return;
            SetWeaponVisibility(player!, false);
            SetWeaponAttack(player!, true);
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                foreach ((var playerId, var itemList) in invisibleEntities)
                    if (player.SteamID !=  playerId)
                        foreach (var item in itemList)
                            info.TransmitEntities.Remove(item);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            Event.enableTransmit = true;
            SetPlayerVisibility(player, false);
            SetWeaponVisibility(player, false);
            SetWeaponAttack(player, true);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SetPlayerVisibility(player, true);
            SetWeaponVisibility(player, true);
            SetWeaponAttack(player, false);
            invisibleEntities.Remove(player.SteamID);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    UpdateHUD(player);
            }
        }

        private static void SetPlayerVisibility(CCSPlayerController player, bool visible)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null)
            {
                var color = visible ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 255, 255, 255);
                var shadowStrength = visible ? 1.0f : 0.0f;

                playerPawn.Render = color;
                playerPawn.ShadowStrength = shadowStrength;
                Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            }
        }

        private static void SetWeaponVisibility(CCSPlayerController player, bool visible)
        {
            if (!Instance.IsPlayerValid(player)) return;
            var playerPawn = player.PlayerPawn.Value!;
            if (playerPawn.WeaponServices == null) return;

            // var color = visible ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 255, 255, 255);
            // var shadowStrength = visible ? 1.0f : 0.0f;

            invisibleEntities.Remove(player.SteamID);
            foreach (var weapon in playerPawn.WeaponServices.MyWeapons)
            {
                if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                {
                    if (!visible)
                    {
                        if (invisibleEntities.TryGetValue(player.SteamID, out var items))
                        {
                            if (!items.Contains(weapon.Index))
                                items.Add(weapon.Index);
                        }
                        else
                            invisibleEntities.Add(player.SteamID, [weapon.Index]);
                    }
                    /*
                    weapon.Value.Render = color;
                    weapon.Value.ShadowStrength = shadowStrength;
                    Utilities.SetStateChanged(weapon.Value, "CBaseModelEntity", "m_clrRender");*/
                }
            }

            if (visible)
                invisibleEntities.Remove(player.SteamID);
        }

        private static void SetWeaponAttack(CCSPlayerController player, bool disableWeapon)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player?.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

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

        private static void UpdateHUD(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            var weapon = pawn.WeaponServices.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid || !disabledWeapons.Contains(weapon.DesignerName)) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("disabled_weapon")}</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FFFFFF", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}