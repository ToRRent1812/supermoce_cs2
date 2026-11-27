using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Supermoce
{
    public enum ToastKind
    {
        Info,
        Damage,
        Poison,
        Blind,
        Slow,
        Disarm,
        Steal,
        Money,
        Teleport,
        Jam,
        Bomb,
    }

    /// <summary>
    /// Toast store. The spectator row is the only toast surface now: it shows the
    /// newest toast instead of its skill text, then falls back on expiry.
    /// </summary>
    public static class ToastService
    {
        public const float DefaultDuration = 3f;

        private sealed class Entry
        {
            public string Text = "";
            public float Expires;
        }

        private static readonly Dictionary<int, Entry> _entries = [];

        public static void Show(CCSPlayerController? player, ToastKind kind, string text, float duration = DefaultDuration)
        {
            if (player == null || !player.IsValid || player.IsBot) return;

            _entries[player.Slot] = new Entry
            {
                Text = text,
                Expires = Server.CurrentTime + duration,
            };
        }

        public static void ShowAll(ToastKind kind, string text, float duration = DefaultDuration)
        {
            foreach (var player in SkillUtils.CachedHumans)
                Show(player, kind, text, duration);
        }

        public static bool TryGetActive(int slot, out string text)
        {
            if (_entries.TryGetValue(slot, out var entry) && entry.Expires > Server.CurrentTime)
            {
                text = entry.Text;
                return true;
            }

            text = "";
            return false;
        }

        public static void Tick()
        {
            if (_entries.Count == 0) return;

            float now = Server.CurrentTime;
            foreach (var (slot, entry) in _entries.ToArray())
                if (entry.Expires <= now)
                    _entries.Remove(slot);
        }

        public static void ClearSlot(int slot) => _entries.Remove(slot);

        public static void ClearAll() => _entries.Clear();
    }
}
