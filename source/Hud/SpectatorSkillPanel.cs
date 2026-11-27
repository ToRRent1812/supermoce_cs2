using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PanoramaManager;

namespace Supermoce
{
    public static class SpectatorSkillPanel
    {
        private sealed class State
        {
            public int TargetSlot = -1;
            public string Text = "";
            public int Tone = -1;
            public bool? Toast;
            public bool Hidden = true;
            public float NextPoll;
            public float NextOpen;
        }

        private static readonly Dictionary<int, State> _states = [];
        private static bool _roundOver;

        public static void Tick()
        {
            var hud = PanoramaBootstrap.SpectatorSkill;
            if (hud == null) return;

            foreach (var player in SkillUtils.CachedHumans)
            {
                if (player == null || !player.IsValid) continue;

                if (_roundOver)
                {
                    Hide(player, hud);
                    continue;
                }

                // The row is shared with toasts; the newest toast wins over the skill text.
                if (ToastService.TryGetActive(player.Slot, out var toastText))
                {
                    Render(player, hud, GetState(player.Slot), toastText, HudPalette.Danger, true);
                    continue;
                }

                if (player.PawnIsAlive || !SkillHudPanel.ShouldShowHud())
                {
                    Hide(player, hud);
                    continue;
                }

                var state = GetState(player.Slot);
                var now = Server.CurrentTime;
                if (now >= state.NextPoll)
                {
                    state.NextPoll = now + 0.5f;
                    state.TargetSlot = ResolveTargetSlot(player);
                }

                var target = state.TargetSlot >= 0
                    ? Utilities.GetPlayerFromSlot(state.TargetSlot)
                    : null;
                if (target == null || !target.IsValid)
                {
                    Hide(player, hud);
                    continue;
                }

                var info = SkillUtils.GetPlayerInfo(target) ?? Event.EnsurePlayerInfo(target);
                var skill = info == null ? null : SkillData.Find(info.Skill);
                if (info == null || skill == null)
                {
                    Hide(player, hud);
                    continue;
                }

                Render(player, hud, state, FormatText(info, skill, target), skill.Tone, false);
            }
        }

        public static void Hide(CCSPlayerController player, PanelHandle hud)
        {
            _states.Remove(player.Slot);
            if (!hud.IsOpenFor(player)) return;

            hud.SetClassFor(player, HudContract.SpectatorSkillRoot, HudContract.HiddenClass, true);
            hud.Close(player);
        }

        public static void HideAll()
        {
            var hud = PanoramaBootstrap.SpectatorSkill;
            if (hud == null)
            {
                _states.Clear();
                return;
            }

            foreach (var player in SkillUtils.CachedHumans)
                if (player != null && player.IsValid)
                    Hide(player, hud);

            _states.Clear();
        }

        /// <summary>Round start hands out a fresh pawn; drop any leftover row or toast.</summary>
        public static void ResetForSpawn(CCSPlayerController player)
        {
            ToastService.ClearSlot(player.Slot);
            if (PanoramaBootstrap.SpectatorSkill is { } hud)
                Hide(player, hud);
        }

        public static void OnRoundEnd()
        {
            _roundOver = true;
            HideAll();
            ToastService.ClearAll();
        }

        public static void OnRoundStart()
        {
            _roundOver = false;
            ToastService.ClearAll();
        }

        public static void ClearSlot(int slot) => _states.Remove(slot);

        public static void ClearAll()
        {
            _states.Clear();
            _roundOver = false;
        }

        private static void Render(CCSPlayerController player, PanelHandle hud, State state, string text, int tone, bool toast)
        {
            float now = Server.CurrentTime;
            if (!hud.IsOpenFor(player) && now >= state.NextOpen)
            {
                hud.Open(player);
                state.NextOpen = now + 5f;
            }

            if (state.Hidden)
            {
                hud.SetClassFor(player, HudContract.SpectatorSkillRoot, HudContract.HiddenClass, false);
                state.Hidden = false;
            }

            if (state.Toast != toast)
            {
                hud.SetClassFor(player, HudContract.SpectatorSkillRoot, HudContract.ToastClass, toast);
                state.Toast = toast;
            }

            if (state.Text != text)
            {
                hud.SetVariableFor(player, "sm_spec", text);
                state.Text = text;
            }

            if (state.Tone != tone)
            {
                if (state.Tone >= 0)
                    hud.SetClassFor(player, "SmSpecText", HudPalette.ClassFor(state.Tone), false);
                hud.SetClassFor(player, "SmSpecText", HudPalette.ClassFor(tone), true);
                state.Tone = tone;
            }
        }

        private static State GetState(int slot)
        {
            if (!_states.TryGetValue(slot, out var state))
            {
                state = new State();
                _states[slot] = state;
            }

            return state;
        }

        private static string FormatText(SkillPlayerInfo info, SkillInfo skill, CCSPlayerController target)
        {
            if (!string.IsNullOrWhiteSpace(info.RandomPercentage))
                return $"{skill.Name} ({info.RandomPercentage})";

            if (SkillData.ActiveSkillConfigs.ContainsKey(info.Skill) &&
                ActiveSkillFramework.TryGetSkillState(info.Skill, target, out var state) &&
                state != null)
            {
                float remaining = ActiveSkillFramework.GetRemainingCooldown(info.Skill, target);
                string cooldown = remaining > 0f
                    ? remaining.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s"
                    : "0s";
                return $"{skill.Name} ({cooldown})";
            }

            return skill.Name;
        }

        private static int ResolveTargetSlot(CCSPlayerController player)
        {
            var observerPawn = player.ObserverPawn?.Value;
            var handle = observerPawn?.ObserverServices?.ObserverTarget?.Value?.Handle;
            if (handle == null || handle == nint.Zero)
                handle = player.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle;
            if (handle == null) return -1;

            var target = SkillUtils.CachedPlayers.FirstOrDefault(p => p.Pawn?.Value?.Handle == handle);
            return target?.Slot ?? -1;
        }
    }
}
