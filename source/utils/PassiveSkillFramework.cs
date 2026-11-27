using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using Supermoce.src.player;

namespace Supermoce
{
    public static class PassiveSkillFramework
    {
        private static readonly ConcurrentDictionary<(Skills, int), PassiveSkillState> _skillStates = [];
        private static readonly ConcurrentDictionary<(Skills, int), int> _randomRolls = [];

        public class PassiveSkillState
        {
            public int Slot { get; set; }
            public string? RandomValue { get; set; }
        }

        public static void OnSkillEnabled(Skills skill, CCSPlayerController player, PassiveSkillConfig? config = null)
        {
            var key = (skill, player.Slot);

            int randomRoll = GenerateRandomRoll(config);
            _randomRolls.AddOrUpdate(key, randomRoll, (k, existing) => randomRoll);

            if (config != null && config.MaxValue >= config.MinValue)
            {
                var formattedValue = config.FormatValue(randomRoll);
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo != null)
                {
                    playerInfo.RandomPercentage = formattedValue;
                }

                _skillStates.AddOrUpdate(key,
                    new PassiveSkillState { Slot = player.Slot, RandomValue = playerInfo?.RandomPercentage ?? formattedValue },
                    (k, existing) => new PassiveSkillState { Slot = player.Slot, RandomValue = playerInfo?.RandomPercentage ?? formattedValue });
            }
            else
            {
                _skillStates.TryAdd(key, new PassiveSkillState { Slot = player.Slot });
            }
        }

        public static int GetRandomRoll(Skills skill, CCSPlayerController player, PassiveSkillConfig? config)
        {
            var key = (skill, player.Slot);
            if (_randomRolls.TryGetValue(key, out var roll))
                return roll;

            int newRoll = GenerateRandomRoll(config);
            _randomRolls.TryAdd(key, newRoll);
            return newRoll;
        }

        private static int GenerateRandomRoll(PassiveSkillConfig? config)
        {
            if (config == null || config.MaxValue < config.MinValue)
                return 0;

            if (config.MaxValue == config.MinValue)
                return config.MinValue;

            int range = (config.MaxValue - config.MinValue) / Math.Max(config.Step, 1) + 1;
            return config.MinValue + (Supermoce.Instance?.Random.Next(range) ?? 0) * Math.Max(config.Step, 1);
        }

        public static void OnSkillDisabled(Skills skill, CCSPlayerController player)
        {
            var key = (skill, player.Slot);
            _skillStates.TryRemove(key, out _);
            _randomRolls.TryRemove(key, out _);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo != null)
            {
                playerInfo.RandomPercentage = null;
            }
        }

        public static void OnNewRound()
        {
            _skillStates.Clear();
            _randomRolls.Clear();
        }
    }
}
