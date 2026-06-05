using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class WeaponsSwap : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.WeaponsSwap;
        private static readonly ConcurrentDictionary<ulong, PlayerSkillInfo> SkillPlayerInfo = [];
        private static readonly object setLock = new();

        private static readonly string[] primaryWeapons = [ "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7",
        "weapon_ump45", "weapon_p90", "weapon_mp5sd", "weapon_famas", "weapon_galilar", "weapon_m4a1",
        "weapon_m4a1_silencer", "weapon_ak47", "weapon_aug", "weapon_sg553", "weapon_ssg08", "weapon_awp",
        "weapon_scar20", "weapon_g3sg1", "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff",
        "weapon_m249", "weapon_negev" ];
        private static readonly string[] grenadeWeapons = [ "weapon_flashbang", "weapon_hegrenade", "weapon_smokegrenade", "weapon_decoy", "weapon_molotov", "weapon_incgrenade" ];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Chachmęciarz",
                "Na żądanie kradniesz broń główną i granaty losowego przeciwnika",
                "#c7e03a",
                minCooldown: 20,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            ActiveSkillFramework.OnNewRound();
            lock (setLock)
                SkillPlayerInfo.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);

            SkillPlayerInfo.TryAdd(player.SteamID, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                LastClick = DateTime.MinValue,
                FindedEnemy = true,
                HaveWeapon = true,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
            SkillPlayerInfo.TryRemove(player.SteamID, out _);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            if (SkillUtils.IsFreezetime()) return;

            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;

            if (!SkillPlayerInfo.TryGetValue(player.SteamID, out var skillInfo)) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
            {
                skillInfo.LastClick = DateTime.Now;
                return;
            }

            CCSPlayerController? enemy = GetRandomEnemy(player);
            if (enemy == null)
            {
                SkillUtils.PrintToChat(player, $"Nie znaleziono odpowiedniego wroga", true);
                skillInfo.FindedEnemy = false;
                skillInfo.LastClick = DateTime.Now;
                return;
            }

            string[]? enemyWeapons = GetWeapons(enemy);
            if (enemyWeapons == null)
            {
                SkillUtils.PrintToChat(player, $"Wylosowany wróg nie ma odpowiedniej broni", true);
                skillInfo.FindedEnemy = true;
                skillInfo.HaveWeapon = false;
                skillInfo.LastClick = DateTime.Now;
                return;
            }

            string[] stolenWeapons = [.. enemyWeapons.Where(IsStealableWeapon)];
            if (stolenWeapons.Length == 0)
            {
                SkillUtils.PrintToChat(player, $"Wylosowany wróg nie ma odpowiedniej broni", true);
                skillInfo.FindedEnemy = true;
                skillInfo.HaveWeapon = false;
                skillInfo.LastClick = DateTime.Now;
                return;
            }

            skillInfo.HaveWeapon = true;
            skillInfo.FindedEnemy = true;
            skillInfo.LastClick = DateTime.Now;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            RemoveStolenWeapons(enemy, stolenWeapons);
            Server.NextFrame(() =>
            {
                GiveWeapons(player, stolenWeapons);
                Instance?.AddTimer(0.3f, () => enemy.ExecuteClientCommand("slot2"));
            });

            SkillUtils.PrintToChat(enemy, $"Wróg ukradł Ci sprzęt.", true);
            
        }

        private static string[]? GetWeapons(CCSPlayerController player)
        {
            if (player == null || player.PlayerPawn == null || player.PlayerPawn.Value == null)
                return null;

            ConcurrentBag<string> playerWeapons = [];
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return null;
            if (pawn.WeaponServices == null) return null;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
                if (weapon.Value != null && weapon.Value.IsValid)
                    playerWeapons.Add(SkillUtils.GetDesignerName(weapon.Value));
            return playerWeapons.Count == 0 ? null : [.. playerWeapons];
        }

        private static void GiveWeapons(CCSPlayerController player, string[]? weapons)
        {
            if (weapons == null || player == null || !player.IsValid || !player.PawnIsAlive) return;
            foreach (var weapon in weapons)
                player.GiveNamedItem(weapon);
        }

        private static void RemoveStolenWeapons(CCSPlayerController player, string[] weapons)
        {
            if (player == null || !player.IsValid) return;
            foreach (var weapon in weapons)
                player.RemoveItemByDesignerName(weapon);
        }

        private static bool IsStealableWeapon(string weapon)
        {
            return primaryWeapons.Contains(weapon) || grenadeWeapons.Contains(weapon);
        }

        private static CCSPlayerController? GetRandomEnemy(CCSPlayerController player)
        {
            CCSPlayerController[] enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0) return null;
            return enemies[(Instance?.Random.Next(enemies.Length)) ?? 0];
        }

        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public DateTime LastClick { get; set; }
            public bool FindedEnemy { get; set; }
            public bool HaveWeapon { get; set; }
        }
    }
}
