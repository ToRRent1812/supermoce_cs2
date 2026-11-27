using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PanoramaManager;
using Supermoce.src.player;

namespace Supermoce
{
    public static class SkillHudPanel
    {
        private const float RollFlashInterval = 0.1f;
        private const float FreezeGateSeconds = 13f;

        private enum Phase { Hidden, Drawing, Ready, Cooling, Passive, Override }

        private const string Unwritten = "\u0000";

        private readonly record struct HudState(Phase Phase, string Name, string Roll, string Desc, string Status, int Tone);

        private sealed class Rendered
        {
            public Phase Phase = Phase.Hidden;
            public string Name = "";
            public string Roll = "";
            public string Desc = "";
            public string Status = "";
            public bool NoStatus;
            public int Tone = -1;
            public bool Hidden = true;
            public float ResyncAt;
        }

        private sealed class Override
        {
            public string Status = "";
            public float Expires;
        }

        private sealed class DrawingState
        {
            public string Name = "";
            public int Tone;
            public float NextChange;
        }

        private static readonly Dictionary<int, DrawingState> _drawing = [];
        private static readonly Dictionary<int, Rendered> _rendered = [];
        private static readonly Dictionary<int, Override> _overrides = [];
        private static readonly Dictionary<int, float> _nextOpenAttempt = [];

        public static void Tick()
        {
            var hud = PanoramaBootstrap.SkillHud;
            if (hud == null) return;

            foreach (var player in SkillUtils.CachedHumans)
            {
                if (player == null || !player.IsValid) continue;

                Event.EnsurePlayerInfo(player);

                if (!ShouldShowHud())
                {
                    if (hud.IsOpenFor(player)) hud.Close(player);
                    continue;
                }

                if (!hud.IsOpenFor(player) &&
                    (!_nextOpenAttempt.TryGetValue(player.Slot, out var next) || next <= Server.CurrentTime))
                {
                    hud.Open(player);
                    MarkOpened(player.Slot);
                    _nextOpenAttempt[player.Slot] = Server.CurrentTime + 5f;
                }

                Render(player, hud, BuildState(player));
            }
        }

        public static void Refresh(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            if (PanoramaBootstrap.SkillHud is { } hud)
                Render(player, hud, BuildState(player));
        }

        public static void ShowStatus(CCSPlayerController? player, Skills skill, string status, float ttl = 0.3f)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            _overrides[player.Slot] = new Override
            {
                Status = status,
                Expires = Server.CurrentTime + ttl,
            };
        }

        public static void ClearSlot(int slot)
        {
            _rendered.Remove(slot);
            _overrides.Remove(slot);
            _nextOpenAttempt.Remove(slot);
            _drawing.Remove(slot);
        }

        public static void MarkOpened(int slot)
        {
            if (!_rendered.TryGetValue(slot, out var rendered))
            {
                rendered = new Rendered();
                _rendered[slot] = rendered;
            }

            if (PanoramaBootstrap.SkillHud is { } hud)
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player != null && player.IsValid)
                {
                    if (rendered.Tone >= 0)
                    {
                        hud.SetClassFor(player, "SmHudName", HudPalette.ClassFor(rendered.Tone), false);
                        hud.SetClassFor(player, HudContract.SkillHudRoot, $"bl-tone-{rendered.Tone}", false);
                    }
                    if (rendered.Phase != Phase.Hidden)
                        hud.SetClassFor(player, HudContract.SkillHudRoot, PhaseClass(rendered.Phase), false);
                    hud.SetClassFor(player, HudContract.SkillHudRoot, "sm-nostatus", false);
                    hud.SetClassFor(player, HudContract.SkillHudRoot, HudContract.HiddenClass, true);
                }
            }

            rendered.Hidden = true;
            rendered.Phase = Phase.Hidden;
            rendered.Name = Unwritten;
            rendered.Roll = Unwritten;
            rendered.Desc = Unwritten;
            rendered.Status = Unwritten;
            rendered.NoStatus = false;
            rendered.Tone = -1;
            rendered.ResyncAt = Server.CurrentTime + 0.5f;
        }

        public static void ClearCache()
        {
            _rendered.Clear();
            _overrides.Clear();
            _nextOpenAttempt.Clear();
            _drawing.Clear();
        }

        public static bool ShouldShowHud()
        {
            if (GameState.Warmup) return false;
            if (!GameState.FreezePeriod) return true;

            if (GameState.RoundStartTime <= 0f)
                return !GameState.TeamIntroPeriod; // no reliable timer: hide the intro

            // RoundStartTime is when freeze time ends, so it is in the future here.
            float remaining = GameState.RoundStartTime - Server.CurrentTime;
            return !GameState.TeamIntroPeriod && remaining <= FreezeGateSeconds;
        }

        private static HudState BuildState(CCSPlayerController player)
        {
            if (!ShouldShowHud())
                return Hidden();

            var info = SkillUtils.GetPlayerInfo(player);
            if (info == null)
                return Hidden();

            // The buy menu covers the HUD; hide underneath it and come back on close.
            var pawn = player.PlayerPawn?.Value;
            if (pawn != null && pawn.IsValid && pawn.IsBuyMenuOpen)
                return Hidden();

            if (info.IsDrawing)
                return BuildDrawingState(player);

            if (!player.PawnIsAlive)
                return Hidden();

            if (_overrides.TryGetValue(player.Slot, out var ov) && ov.Expires > Server.CurrentTime)
            {
                var ovSkill = SkillData.Find(info.Skill);
                return new HudState(Phase.Override, ovSkill?.Name ?? info.Skill.ToString(), "",
                    ovSkill?.Description ?? "", ov.Status, ovSkill?.Tone ?? HudPalette.Info);
            }

            var skill = SkillData.Find(info.Skill);
            if (skill == null)
                return Hidden();

            if (SkillData.ActiveSkillConfigs.ContainsKey(info.Skill) &&
                ActiveSkillFramework.TryGetSkillState(info.Skill, player, out var state) && state != null)
            {
                float elapsed = Server.CurrentTime - state.LastUseTime;
                float remaining = Math.Max(state.CooldownSeconds - elapsed, 0);

                if (remaining > 0)
                {
                    string seconds = remaining.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    return new HudState(Phase.Cooling, skill.Name, "", skill.Description,
                        $"Poczekaj {seconds} sek.", skill.Tone);
                }

                return new HudState(Phase.Ready, skill.Name, "", skill.Description,
                    "Wciśnij INSPEKT by użyć", skill.Tone);
            }

            bool passive = SkillData.PassiveSkillConfigs.ContainsKey(info.Skill);
            return new HudState(passive ? Phase.Passive : Phase.Ready, skill.Name,
                passive ? info.RandomPercentage ?? "" : "", skill.Description, "", skill.Tone);
        }

        private static HudState BuildDrawingState(CCSPlayerController player)
        {
            int slot = player.Slot;
            float now = Server.CurrentTime;

            if (!_drawing.TryGetValue(slot, out var state) || now >= state.NextChange)
            {
                var skills = SkillData.Skills.ToArray();
                var skill = skills.Length == 0 ? null : skills[Supermoce.Instance?.Random.Next(skills.Length) ?? 0];
                state = new DrawingState
                {
                    Name = skill?.Name ?? "?",
                    Tone = skill?.Tone ?? HudPalette.Info,
                    NextChange = now + RollFlashInterval,
                };
                _drawing[slot] = state;
            }

            return new HudState(Phase.Drawing, state.Name, "", "Losowanie supermocy...", "", state.Tone);
        }

        private static HudState Hidden() => new(Phase.Hidden, "", "", "", "", 0);

        private static void Render(CCSPlayerController player, PanelHandle hud, HudState state)
        {
            // Never show an empty card.
            if (state.Phase != Phase.Drawing && string.IsNullOrWhiteSpace(state.Name))
            {
                state = Hidden();
            }

            int slot = player.Slot;
            if (!_rendered.TryGetValue(slot, out var r))
            {
                r = new Rendered();
                _rendered[slot] = r;
            }

            if (Server.CurrentTime >= r.ResyncAt)
            {
                r.Name = Unwritten;
                r.Roll = Unwritten;
                r.Desc = Unwritten;
                r.Status = Unwritten;
                r.ResyncAt = Server.CurrentTime + 0.5f;
            }

            if (state.Phase == Phase.Hidden)
            {
                if (!r.Hidden)
                {
                    hud.SetClassFor(player, HudContract.SkillHudRoot, HudContract.HiddenClass, true);
                    if (r.Phase != Phase.Hidden) hud.SetClassFor(player, HudContract.SkillHudRoot, PhaseClass(r.Phase), false);
                    r.Hidden = true;
                    r.Phase = Phase.Hidden;
                    _drawing.Remove(slot);
                }
                return;
            }

            if (r.Hidden)
            {
                hud.SetClassFor(player, HudContract.SkillHudRoot, HudContract.HiddenClass, false);
                r.Hidden = false;
            }

            if (r.Phase != state.Phase)
            {
                if (r.Phase != Phase.Hidden) hud.SetClassFor(player, HudContract.SkillHudRoot, PhaseClass(r.Phase), false);
                hud.SetClassFor(player, HudContract.SkillHudRoot, PhaseClass(state.Phase), true);
                r.Phase = state.Phase;
                r.Hidden = false;
            }

            if (r.Name != state.Name) { hud.SetVariableFor(player, "sm_name", state.Name); r.Name = state.Name; }
            if (r.Roll != state.Roll) { hud.SetVariableFor(player, "sm_roll", state.Roll); r.Roll = state.Roll; }
            if (r.Desc != state.Desc) { hud.SetVariableFor(player, "sm_desc", state.Desc); r.Desc = state.Desc; }
            if (r.Status != state.Status) { hud.SetVariableFor(player, "sm_status", state.Status); r.Status = state.Status; }

            bool noStatus = string.IsNullOrWhiteSpace(state.Status);
            if (r.NoStatus != noStatus)
            {
                hud.SetClassFor(player, HudContract.SkillHudRoot, "sm-nostatus", noStatus);
                r.NoStatus = noStatus;
            }

            if (r.Tone != state.Tone)
            {
                string tone = HudPalette.ClassFor(state.Tone);
                if (r.Tone >= 0)
                {
                    string oldTone = HudPalette.ClassFor(r.Tone);
                    hud.SetClassFor(player, "SmHudName", oldTone, false);
                    hud.SetClassFor(player, HudContract.SkillHudRoot, $"bl-tone-{r.Tone}", false);
                }
                hud.SetClassFor(player, "SmHudName", tone, true);
                hud.SetClassFor(player, HudContract.SkillHudRoot, $"bl-tone-{state.Tone}", true);
                r.Tone = state.Tone;
            }

            if (state.Phase != Phase.Drawing)
                _drawing.Remove(slot);
        }

        private static string PhaseClass(Phase phase) => phase switch
        {
            Phase.Drawing => "sm-drawing",
            Phase.Ready => "sm-ready",
            Phase.Cooling => "sm-cooling",
            Phase.Passive => "sm-passive",
            Phase.Override => "sm-override",
            _ => HudContract.HiddenClass,
        };
    }
}
