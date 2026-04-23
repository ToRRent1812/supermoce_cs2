using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using System.Linq;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Phoenix : ISkill
    {
        private const Skills skillName = Skills.Phoenix;
        private static readonly object setLock = new();
        private static readonly string[] grenadeNames = [ "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy", "weapon_taser", "weapon_zeus" ];
        private class SavedLoadout
        {
            public string[] Weapons { get; set; } = [];
            public string ActiveWeapon { get; set; } = string.Empty;
            public ConcurrentDictionary<string, int> Clips { get; set; } = new();
            public ConcurrentDictionary<string, int> Grenades { get; set; } = new();
        }

        private static readonly ConcurrentDictionary<ulong, SavedLoadout> savedLoadouts = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Feniks", "Jeżeli nie jesteś ostatni żywy w drużynie, masz szansę odrodzić się po śmierci", "#ff5C0A", 1);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
            {
                int aliveCount = Utilities.GetPlayers().Count(p => p.TeamNum == player.TeamNum && p.PawnHealth > 0);
                if (Instance?.Random.NextDouble() <= playerInfo.SkillChance && aliveCount > 1)
                {
                    // capture current loadout before respawn
                    try
                    {
                        var pawn = player.PlayerPawn.Value;
                        if (pawn != null && pawn.IsValid && pawn.WeaponServices != null)
                        {
                            var bag = new ConcurrentBag<string>();
                            var clips = new ConcurrentDictionary<string, int>();
                            var grenades = new ConcurrentDictionary<string, int>();
                            foreach (var item in pawn.WeaponServices.MyWeapons)
                            {
                                if (item != null && item.IsValid && item.Value != null && item.Value.IsValid)
                                {
                                    string name = SkillUtils.GetDesignerName(item.Value);
                                    if (string.IsNullOrEmpty(name)) continue;

                                    if (grenadeNames.Contains(name))
                                    {
                                        grenades.AddOrUpdate(name, 1, (k, old) => old + 1);
                                    }
                                    else
                                    {
                                        bag.Add(name);
                                        try { clips.TryAdd(name, item.Value.Clip1); } catch { }
                                    }
                                }
                            }

                            string activeName = string.Empty;
                            var active = pawn.WeaponServices.ActiveWeapon?.Value;
                            if (active != null && active.IsValid)
                                activeName = SkillUtils.GetDesignerName(active);

                            if (!bag.IsEmpty || !grenades.IsEmpty)
                                savedLoadouts[player.SteamID] = new SavedLoadout { Weapons = bag.ToArray(), ActiveWeapon = activeName, Clips = clips, Grenades = grenades };
                        }
                    }
                    catch { }

                    lock (setLock)
                    {
                        player.Respawn();
                        Instance?.AddTimer(.2f, () =>
                        {
                            try
                            {
                                player.Respawn();
                                RestoreLoadout(player);
                            }
                            catch { }
                        });
                    }
                }
            }
        }

        private static void RestoreLoadout(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            if (!savedLoadouts.TryRemove(player.SteamID, out var loadout)) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;

            // remove default weapons and give saved ones
            Server.NextFrame(() =>
            {
                try
                {
                    player.RemoveWeapons();
                    // always give knife first to avoid empty hands
                    player.GiveNamedItem("weapon_knife");
                    foreach (var w in loadout.Weapons)
                    {
                        if (string.IsNullOrEmpty(w)) continue;
                        player.GiveNamedItem(w);
                    }

                    // restore grenades
                    if (loadout.Grenades != null && !loadout.Grenades.IsEmpty)
                    {
                        foreach (var g in loadout.Grenades)
                        {
                            try
                            {
                                for (int i = 0; i < g.Value; i++)
                                    player.GiveNamedItem(g.Key);
                            }
                            catch { }
                        }
                    }

                    // after a short delay set clips and restore active weapon
                    Instance?.AddTimer(0.4f, () =>
                    {
                        try
                        {
                            var pawnNow = player.PlayerPawn?.Value;
                            if (pawnNow == null || !pawnNow.IsValid || pawnNow.WeaponServices == null) return;

                            foreach (var item in pawnNow.WeaponServices.MyWeapons)
                            {
                                if (item == null || !item.IsValid || item.Value == null || !item.Value.IsValid) continue;
                                string name = SkillUtils.GetDesignerName(item.Value);
                                if (string.IsNullOrEmpty(name)) continue;
                                if (loadout.Clips.TryGetValue(name, out var clip))
                                {
                                    try { item.Value.Clip1 = clip; } catch { }
                                }
                            }

                            if (!string.IsNullOrEmpty(loadout.ActiveWeapon))
                            {
                                string slotCmd = MapWeaponToSlot(loadout.ActiveWeapon);
                                if (!string.IsNullOrEmpty(slotCmd))
                                    player.ExecuteClientCommand(slotCmd);
                            }
                        }
                        catch { }
                    });
                }
                catch { }
            });
        }

        private static string MapWeaponToSlot(string designerName)
        {
            if (string.IsNullOrEmpty(designerName)) return string.Empty;
            string[] pistols = [ "weapon_deagle", "weapon_revolver", "weapon_glock", "weapon_usp_silencer", "weapon_cz75a", "weapon_fiveseven", "weapon_p250", "weapon_tec9", "weapon_elite", "weapon_hkp2000" ];
            if (designerName.Contains("knife") || designerName.Contains("bayonet")) return "slot3";
            if (pistols.Contains(designerName)) return "slot2";
            return "slot1";
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(6,14) * 5 ?? 10; //30-70%
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = randomValue.ToString() + "%";
        }
    }
}