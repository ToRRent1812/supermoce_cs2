using CounterStrikeSharp.API;

namespace Supermoce
{
    public static class GameState
    {
        public static bool Warmup { get; private set; }
        public static bool FreezePeriod { get; private set; }
        public static bool TeamIntroPeriod { get; private set; }
        public static int FreezeTime { get; private set; }
        public static float RoundStartTime { get; private set; }
        public static bool IsBombMap { get; private set; }
        public static bool IsHostageMap { get; private set; }

        public static void Refresh()
        {
            var rules = Utilities.GetGameRules();
            if (Supermoce.Instance != null)
                Supermoce.Instance.GameRules = rules;

            if (rules == null || rules.Handle == IntPtr.Zero)
            {
                Warmup = false;
                FreezePeriod = false;
                TeamIntroPeriod = false;
                FreezeTime = 0;
                RoundStartTime = 0f;
                IsBombMap = false;
                IsHostageMap = false;
                return;
            }

            Warmup = rules.WarmupPeriod;
            FreezePeriod = rules.FreezePeriod;
            TeamIntroPeriod = rules.TeamIntroPeriod;
            FreezeTime = rules.FreezeTime;
            RoundStartTime = rules.RoundStartTime;
            IsBombMap = rules.MapHasBombTarget;
            IsHostageMap = rules.MapHasRescueZone;
        }
    }
}
