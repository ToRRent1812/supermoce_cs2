using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class AmmoStrip : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.AmmoStrip;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName,
            "Konfiskata",
            "Pozbawiasz wroga zapasowych magazynków do broni",
            "#da47ff");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.ValidateSkillUse(player, skillName, out var playerInfo))
                return;

            string enemyId = commands[0];
            var enemy = Utilities.GetPlayers().FirstOrDefault(p => p.Index.ToString() == enemyId);
            if (enemy == null)
            {
                SkillUtils.PrintToChat(player, $"Nie znaleziono gracza o takim ID.", true);
                return;
            }

            StripAmmo(player, enemy);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);

            SkillUtils.CreateTargetingMenu(
                player,
                enemy => true,
                null,
                () => Event.SetRandomSkill(player));
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SpecialSkill = Skills.None;
        }

        private static void StripAmmo(CCSPlayerController player, CCSPlayerController enemy)
        {
            SkillUtils.CloseMenu(player);

            if(player == null || enemy == null) return;

            Instance?.AddTimer(.5f, () =>
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo == null) return;

                playerInfo.SpecialSkill = skillName;
                SkillUtils.CloseMenu(player);

                var enemyPawn = enemy.PlayerPawn?.Value;
                if (enemyPawn == null || !enemyPawn.IsValid) return;

                var weaponServices = enemyPawn.WeaponServices;
                if (weaponServices == null) return;

                foreach (var weaponHandle in weaponServices.MyWeapons)
                {
                    if (weaponHandle == null || !weaponHandle.IsValid || weaponHandle.Value == null || !weaponHandle.Value.IsValid)
                        continue;

                    var weapon = weaponHandle.Value;
                    string designerName = weapon.DesignerName;

                    if (designerName.Contains("weapon_knife") ||
                        designerName.Contains("weapon_c4") ||
                        designerName.Contains("weapon_flashbang") ||
                        designerName.Contains("weapon_hegrenade") ||
                        designerName.Contains("weapon_smokegrenade") ||
                        designerName.Contains("weapon_incgrenade") ||
                        designerName.Contains("weapon_molotov") ||
                        designerName.Contains("weapon_decoy"))
                        continue;

                    var cs2Weapon = weapon.As<CCSWeaponBase>();
                    if (cs2Weapon is not { IsValid: true }) continue;

                    cs2Weapon.ReserveAmmo[0] = 0;
                    Utilities.SetStateChanged(cs2Weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
                }

                SkillUtils.PrintToChat(enemy, $"Wróg pozbawił Cię zapasowych magazynków!", true);
            });
        }
    }
}