using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using Supermoce.src.player;

namespace Supermoce
{
    public static class MenuSkillFramework
    {
        private static readonly ConcurrentDictionary<ulong, MenuSkillState> _menuStates = [];

        public class MenuSkillState
        {
            public ulong SteamID { get; set; }
            public Skills ActiveSkill { get; set; }
            public bool HasMenu { get; set; }
        }

        public static void OnSkillEnabled(Skills skill, CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return;

            _menuStates.AddOrUpdate(player.SteamID,
                new MenuSkillState
                {
                    SteamID = player.SteamID,
                    ActiveSkill = skill,
                    HasMenu = true
                },
                (k, existing) => new MenuSkillState
                {
                    SteamID = player.SteamID,
                    ActiveSkill = skill,
                    HasMenu = true
                });

            SkillUtils.CreateMenu(player, skill);
        }

        public static void OnSkillDisabled(CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return;

            SkillUtils.CloseMenu(player);
            _menuStates.TryRemove(player.SteamID, out _);
        }

        public static bool HasActiveMenu(CCSPlayerController player)
        {
            if (player == null)
                return false;

            return _menuStates.TryGetValue(player.SteamID, out var state) && state.HasMenu;
        }

        public static Skills? GetActiveMenuSkill(CCSPlayerController player)
        {
            if (player == null)
                return null;

            if (_menuStates.TryGetValue(player.SteamID, out var state) && state.HasMenu)
                return state.ActiveSkill;

            return null;
        }

        public static void OnTick()
        {
            if (_menuStates.IsEmpty) return;

            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!_menuStates.TryGetValue(player.SteamID, out var state) || !state.HasMenu)
                    continue;

                SkillUtils.UpdateTargetingMenu(player, state.ActiveSkill);
            }
        }

        public static void OnNewRound()
        {
            _menuStates.Clear();
            foreach(var player in SkillUtils.CachedPlayers)
            {
                SkillUtils.CloseMenu(player);
            }
        }
    }
}
