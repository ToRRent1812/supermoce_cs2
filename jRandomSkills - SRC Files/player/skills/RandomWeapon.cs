using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RandomWeapon : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.RandomWeapon;

        private static readonly string[] rifles = [ "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7", "weapon_ump45", "weapon_p90",
        "weapon_mp5sd", "weapon_famas", "weapon_galilar", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_ak47",
        "weapon_aug", "weapon_sg553", "weapon_ssg08", "weapon_awp", "weapon_scar20", "weapon_g3sg1",
        "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249", "weapon_negev" ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Koło Fortuny",
                "Dostajesz losową broń na żądanie",
                "#e0873a",
                minCooldown: 10,
                maxCooldown: 25,
                cooldownStep: 3);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || player.IsValid == false || player.PawnIsAlive == false || Instance?.GameRules?.FreezePeriod == true)
                return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            RemoveAndGiveWeapon(player);
            ActiveSkillFramework.MarkSkillUsed(skillName, player);
        }

        private static void RemoveAndGiveWeapon(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null) return;

            ConcurrentBag<string> playerWeapons = [];
            foreach (var item in pawn.WeaponServices.MyWeapons)
                if (item != null && item.IsValid && item.Value != null && item.Value.IsValid && !string.IsNullOrEmpty(item.Value.DesignerName))
                    playerWeapons.Add(item.Value.DesignerName);

            if (playerWeapons.IsEmpty)
                return;

            ConcurrentBag<string> weaponList = [.. rifles.Where(w => !playerWeapons.Contains(w))];

            if (weaponList.Count == 0)
                return;

            string weapon = weaponList.ToArray()[Instance?.Random.Next(weaponList.Count) ?? 0];

            string? weaponToRemove = playerWeapons.FirstOrDefault(itemName => rifles.Contains(itemName));

            if (!string.IsNullOrEmpty(weaponToRemove))
            {
                foreach (var item in pawn.WeaponServices.MyWeapons)
                {
                    if (item != null && item.IsValid && item.Value != null && item.Value.IsValid && item.Value.DesignerName == weaponToRemove)
                        item.Value.AcceptInput("Kill");
                }
            }

            Instance?.AddTimer(.2f, () =>
            {
                player.GiveNamedItem(weapon);
            });
        }
    }
}