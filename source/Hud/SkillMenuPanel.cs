using CounterStrikeSharp.API.Core;
using PanoramaManager;

namespace Supermoce
{
    public sealed record MenuEntry(string Label, string Payload, string? Subtitle = null);

    public static class SkillMenuPanel
    {
        private sealed class State
        {
            public string Title = "";
            public string Subtitle = "";
            public string Signature = "";
            public List<MenuEntry> Entries = [];
            public Action<CCSPlayerController, string>? OnSelect;
        }

        private static readonly Dictionary<int, State> _states = [];

        public static bool HasOpen(CCSPlayerController? player)
        {
            if (player == null) return false;
            return PanoramaBootstrap.SkillMenu?.IsOpenFor(player) == true;
        }

        public static void Show(
            CCSPlayerController player,
            string title,
            string subtitle,
            IEnumerable<MenuEntry> entries,
            Action<CCSPlayerController, string> onSelect)
        {
            var hud = PanoramaBootstrap.SkillMenu;
            if (hud == null || player == null || !player.IsValid || player.IsBot) return;

            var state = new State
            {
                Title = title,
                Subtitle = subtitle,
                Entries = entries.ToList(),
                Signature = BuildSignature(entries),
                OnSelect = onSelect,
            };
            _states[player.Slot] = state;

            hud.Title = state.Title;
            hud.Subtitle = state.Subtitle;

            hud.Open(player);
            hud.SetClassFor(player, HudContract.SkillMenuRoot, HudContract.HiddenClass, false);
            Apply(hud, player, state);
        }

        public static void Update(CCSPlayerController player, IEnumerable<MenuEntry> entries)
        {
            if (!_states.TryGetValue(player.Slot, out var state)) return;

            var list = entries.ToList();
            string signature = BuildSignature(list);
            if (signature == state.Signature) return;

            state.Entries = list;
            state.Signature = signature;

            if (PanoramaBootstrap.SkillMenu is { } hud)
                Apply(hud, player, state);
        }

        public static void Close(CCSPlayerController? player)
        {
            if (player == null) return;
            _states.Remove(player.Slot);
            PanoramaBootstrap.SkillMenu?.Close(player);
        }

        public static void ClearSlot(int slot) => _states.Remove(slot);

        public static void ClearAll() => _states.Clear();

        public static void OnEvent(PanelEvent e)
        {
            if (e.Action == PanelAction.Close)
            {
                _states.Remove(e.Player.Slot);
                return;
            }
            if (e.Action == PanelAction.Restored &&
                _states.TryGetValue(e.Player.Slot, out var state) &&
                PanoramaBootstrap.SkillMenu is { } hud)
            {
                Apply(hud, e.Player, state);
            }
        }

        private static void Apply(PanelHandle hud, CCSPlayerController player, State state)
        {
            var items = state.Entries.Select((entry, index) => new MenuItem(
                Id: index.ToString(),
                Title: entry.Label,
                Subtitle: entry.Subtitle,
                OnSelect: e => state.OnSelect?.Invoke(e.Player, entry.Payload),
                Tag: entry.Payload));

            hud.SetItems(items);

            hud.Refresh(player);
        }

        private static string BuildSignature(IEnumerable<MenuEntry> entries) =>
            string.Join('\u0001', entries.Select(e => $"{e.Label}|{e.Payload}|{e.Subtitle}"));
    }
}
