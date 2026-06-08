using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class QuickShot : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.QuickShot;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Szybkostrzelność",
                "Strzelasz szybciej",
                "#8a42f5",
                minValue: 10,
                maxValue: 30,
                step: 2,
                customValueFormatter: (value) => $"+{value}%");
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (player.PlayerPawn?.Value?.Health <= 0) continue;
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill != skillName) continue;

                float rateMultiplier = playerInfo.SkillChance ?? 1f;
                if (rateMultiplier <= 1f) continue;

                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid) continue;
                var weaponServices = pawn.WeaponServices;
                if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid) continue;

                var weapon = weaponServices.ActiveWeapon.Value;
                if (weapon == null || !weapon.IsValid) continue;

                if (weapon.NextPrimaryAttackTick > Server.TickCount)
                {
                    int remainingPrimary = weapon.NextPrimaryAttackTick - Server.TickCount;
                    weapon.NextPrimaryAttackTick = Server.TickCount + Math.Max(1, (int)(remainingPrimary / rateMultiplier));
                    Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                }

                if (weapon.NextSecondaryAttackTick > Server.TickCount)
                {
                    int remainingSecondary = weapon.NextSecondaryAttackTick - Server.TickCount;
                    weapon.NextSecondaryAttackTick = Server.TickCount + Math.Max(1, (int)(remainingSecondary / rateMultiplier));
                    Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
                int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                playerInfo.SkillChance = 1f + randomValue / 100f;
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 1f;
        }
    }
}