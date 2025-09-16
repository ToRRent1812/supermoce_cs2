using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class WeaponsSwap : ISkill
    {
        private const Skills skillName = Skills.WeaponsSwap;
        private static int cd = 10;
        private static readonly Dictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];

        private static readonly string[] weapons = [ "weapon_deagle", "weapon_revolver", "weapon_glock", "weapon_usp_silencer",
        "weapon_cz75a", "weapon_fiveseven", "weapon_p250", "weapon_tec9", "weapon_elite", "weapon_hkp2000",
        "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7", "weapon_ump45", "weapon_p90",
        "weapon_mp5sd", "weapon_famas", "weapon_galilar", "weapon_m4a4", "weapon_m4a1_silencer", "weapon_ak47",
        "weapon_aug", "weapon_sg553", "weapon_ssg08", "weapon_awp", "weapon_scar20", "weapon_g3sg1",
        "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249", "weapon_negev" ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            cd = Instance.Random.Next(4, 10) * 5;
            SkillPlayerInfo.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo[player.SteamID] = new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                CanUse = true,
                Cooldown = DateTime.MinValue,
                LastClick = DateTime.MinValue,
                FindedEnemy = true,
                HaveWeapon = true,
            };
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SkillPlayerInfo.Remove(player.SteamID);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
                        if (skillInfo.LastClick.AddSeconds(4) >= DateTime.Now)
                            UpdateHUD(player, skillInfo, true);
                        else
                            UpdateHUD(player, skillInfo, false);
            }
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo, bool showInfo)
        {
            float cooldown = 0;
            if (skillInfo != null)
            {
                float time = (int)(skillInfo.Cooldown.AddSeconds(cd) - DateTime.Now).TotalSeconds;
                cooldown = Math.Max(time, 0);

                if (cooldown == 0 && skillInfo?.CanUse == false)
                    skillInfo.CanUse = true;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = "";

            if (showInfo)
                remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>{Localization.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}</font>"
                                : skillInfo != null && !skillInfo.FindedEnemy ? $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("hud_info_no_enemy")}</font>"
                                : skillInfo != null && !skillInfo.HaveWeapon ? $"<font class='fontSize-m' color='#FF0000'>{Localization.GetTranslation("weaponsswap_hud_info2")}</font>"
                                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";
            else
                remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>{Localization.GetTranslation("hud_info", $"<font color='#FF0000'>{cooldown}</font>")}</font> <br>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>Wciśnij INSPEKT by użyć</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo))
            {
                if (!player.IsValid || !player.PawnIsAlive) return;
                if (skillInfo.CanUse)
                {
                    CCSPlayerController? enemy = GetRandomEnemy(player);
                    if (enemy == null)
                    {
                        skillInfo.FindedEnemy = false;
                        skillInfo.LastClick = DateTime.Now;
                        return;
                    }

                    string[]? playerWeapon = GetWeapons(player);
                    string[]? enemyWeapon = GetWeapons(enemy);

                    if (enemyWeapon != null && enemyWeapon.FirstOrDefault(w => weapons.Contains(w)) == null)
                    {
                        skillInfo.FindedEnemy = true;
                        skillInfo.HaveWeapon = false;
                        skillInfo.LastClick = DateTime.Now;
                        return;
                    }

                    skillInfo.HaveWeapon = true;
                    skillInfo.FindedEnemy = true;
                    skillInfo.CanUse = false;
                    skillInfo.Cooldown = DateTime.Now;

                    if (enemyWeapon != null && playerWeapon != null)
                    {
                        RemoveC4(player);
                        RemoveC4(enemy);
                        Server.NextFrame(() =>
                        {
                            player.RemoveWeapons();
                            enemy.RemoveWeapons();
                            SkillUtils.TryGiveWeapon(enemy, CsItem.Knife);
                            GiveWeapons(player, enemyWeapon, playerWeapon.Contains("weapon_c4"));
                            if (enemyWeapon.Contains("weapon_c4")) SkillUtils.TryGiveWeapon(enemy, CsItem.C4);
                            if (enemy.Team == CsTeam.Terrorist) SkillUtils.TryGiveWeapon(enemy, CsItem.Glock);
                            else SkillUtils.TryGiveWeapon(enemy, CsItem.USPS);
                            Instance.AddTimer(1f, () => enemy.ExecuteClientCommand("slot2"));
                        });
                        //GiveWeapons(enemy, playerWeapon, enemyWeapon.Contains("weapon_c4"), true);
                    }

                    SkillUtils.PrintToChat(enemy, $" {ChatColors.DarkRed}{player.PlayerName}{ChatColors.LightRed} ukradł Ci sprzęt.", false);
                    playerPawn.ArmorValue = 100;
                    Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");
                }
                else
                    skillInfo.LastClick = DateTime.Now;
            }
        }
        
        private static void RemoveC4(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

            foreach (var item in pawn.WeaponServices.MyWeapons)
                if (item != null && item.IsValid && item.Value != null && item.Value.IsValid && item.Value.DesignerName == "weapon_c4")
                    item.Value.AcceptInput("Kill");
        }

        private static string[]? GetWeapons(CCSPlayerController player)
        {
            if (player == null || player.PlayerPawn == null || player.PlayerPawn.Value == null)
                return null;

            List<string> playerWeapons = [];
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return null;
            if (pawn.WeaponServices == null) return null;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
                if (weapon.Value != null && weapon.Value.IsValid)
                    playerWeapons.Add(SkillUtils.GetDesignerName(weapon.Value));
            return playerWeapons.Count == 0 ? null : [.. playerWeapons];
        }

        private static void GiveWeapons(CCSPlayerController player, string[]? weapons, bool addC4)
        {
            if (weapons == null || player == null || !player.IsValid || !player.PawnIsAlive) return;
            player.GiveNamedItem("weapon_knife");
            foreach (var weapon in weapons)
            {
                if (weapon != "weapon_c4")
                    player.GiveNamedItem(weapon);
                if (addC4)
                    player.GiveNamedItem("weapon_c4");
            }
        }

        private static CCSPlayerController? GetRandomEnemy(CCSPlayerController player)
        {
            CCSPlayerController[] enemies = [.. Utilities.GetPlayers().FindAll(e => e.Team != player.Team && e.PawnIsAlive)];
            if (enemies.Length == 0) return null;
            return enemies[Instance.Random.Next(enemies.Length)];
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool CanUse { get; set; }
            public DateTime Cooldown { get; set; }
            public DateTime LastClick { get; set; }
            public bool FindedEnemy { get; set; }
            public bool HaveWeapon { get; set; }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#c7e03a", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}