using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using Supermoce.src.player;

namespace Supermoce
{
    public static class MenuSkillFramework
    {
        private static readonly ConcurrentDictionary<int, MenuSkillState> _menuStates = [];

        public class MenuSkillState
        {
            public int Slot { get; set; }
            public Skills ActiveSkill { get; set; }
            public bool HasMenu { get; set; }
        }

        public static void OnSkillEnabled(Skills skill, CCSPlayerController player)
        {
            if (player == null || !player.IsValid)
                return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo != null)
                playerInfo.SkillChance = 0;

            _menuStates.AddOrUpdate(player.Slot,
                new MenuSkillState
                {
                    Slot = player.Slot,
                    ActiveSkill = skill,
                    HasMenu = true
                },
                (k, existing) => new MenuSkillState
                {
                    Slot = player.Slot,
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
            _menuStates.TryRemove(player.Slot, out _);
        }

        public static void OnTick()
        {
            if (_menuStates.IsEmpty) return;

            foreach (var player in SkillUtils.CachedHumans)
            {
                if (!_menuStates.TryGetValue(player.Slot, out var state) || !state.HasMenu)
                    continue;

                SkillUtils.UpdateTargetingMenu(player, state.ActiveSkill);
            }
        }

        public static void OnNewRound()
        {
            _menuStates.Clear();
            foreach(var player in SkillUtils.CachedHumans)
            {
                SkillUtils.CloseMenu(player);
            }
        }
    }
}
