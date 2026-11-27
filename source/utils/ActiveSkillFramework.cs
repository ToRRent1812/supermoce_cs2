using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using Supermoce.src.player;

namespace Supermoce
{
    public static class ActiveSkillFramework
    {
        private static readonly ConcurrentDictionary<(Skills, int), ActiveSkillState> _skillStates = [];

        public class ActiveSkillState
        {
            public int Slot { get; set; }
            public float LastUseTime { get; set; }
            public int CooldownSeconds { get; set; }
            public bool CanUse { get; set; }
        }

        public static void OnSkillEnabled(Skills skill, CCSPlayerController player, ActiveSkillConfig config)
        {
            int cooldown = config.GenerateCooldown();
            var key = (skill, player.Slot);
            
            _skillStates.AddOrUpdate(key,
                new ActiveSkillState
                {
                    Slot = player.Slot,
                    CanUse = true,
                    LastUseTime = -1000000f,
                    CooldownSeconds = cooldown
                },
                (k, existing) => new ActiveSkillState
                {
                    Slot = player.Slot,
                    CanUse = true,
                    LastUseTime = -1000000f,
                    CooldownSeconds = cooldown
                });
        }

        public static void OnSkillDisabled(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.Slot);
            _skillStates.TryRemove(key, out _);
        }

        public static bool CanUseSkill(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.Slot);
            if (!_skillStates.TryGetValue(key, out var state))
                return false;

            var timeElapsed = (int)(Server.CurrentTime - state.LastUseTime);
            return timeElapsed >= state.CooldownSeconds;
        }

        public static bool TryGetSkillState(Skills skill, CCSPlayerController player, out ActiveSkillState? state)
        {
            var key = (skill, player.Slot);
            return _skillStates.TryGetValue(key, out state);
        }

        public static float GetRemainingCooldown(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.Slot);
            if (!_skillStates.TryGetValue(key, out var state)) return 0f;
            if (state.LastUseTime < 0f) return 0f; // sentinel: never used this round

            return Math.Max(state.CooldownSeconds - (Server.CurrentTime - state.LastUseTime), 0f);
        }

        public static void MarkSkillUsed(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.Slot);
            if (_skillStates.TryGetValue(key, out var state))
            {
                state.LastUseTime = Server.CurrentTime;
            }
        }

        public static void OnNewRound()
        {
            _skillStates.Clear();
        }
    }
}
