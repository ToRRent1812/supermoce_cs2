using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaManager;

namespace Supermoce
{
    public static class TeamPowersPanel
    {
        public const float ShowSeconds = 10f;

        private sealed class State
        {
            public float HideAt;
            public float NextRefresh;
            public string Signature = "";
            public int[] Tones = new int[HudContract.TeamRows];
        }

        private static readonly Dictionary<int, State> _states = [];

        public static void ShowForTeams()
        {
            var hud = PanoramaBootstrap.TeamPowers;
            if (hud == null) return;

            foreach (var player in SkillUtils.CachedHumans)
            {
                if (player == null || !player.IsValid) continue;
                if (player.Team is not (CsTeam.CounterTerrorist or CsTeam.Terrorist)) continue;
                Show(player, hud);
            }
        }

        public static void Show(CCSPlayerController player, PanelHandle hud)
        {
            if (player == null || !player.IsValid || player.IsBot) return;

            hud.Open(player);
            hud.SetClassFor(player, HudContract.TeamPowersRoot, HudContract.HiddenClass, false);

            if (!_states.TryGetValue(player.Slot, out var state))
            {
                state = new State();
                for (int i = 0; i < state.Tones.Length; i++) state.Tones[i] = -1;
                _states[player.Slot] = state;
            }

            state.HideAt = Server.CurrentTime + ShowSeconds;
            state.NextRefresh = 0f;
            Render(player, hud, state);
        }

        public static void Tick()
        {
            var hud = PanoramaBootstrap.TeamPowers;
            if (hud == null || _states.Count == 0) return;

            foreach (var (slot, state) in _states.ToArray())
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player == null || !player.IsValid)
                {
                    _states.Remove(slot);
                    continue;
                }

                if (Server.CurrentTime >= state.HideAt)
                {
                    Hide(player, hud);
                    continue;
                }

                if (Server.CurrentTime >= state.NextRefresh)
                {
                    state.NextRefresh = Server.CurrentTime + 0.5f;
                    Render(player, hud, state);
                }
            }
        }

        public static void Hide(CCSPlayerController player, PanelHandle hud)
        {
            hud.SetClassFor(player, HudContract.TeamPowersRoot, HudContract.HiddenClass, true);
            hud.Close(player);
            _states.Remove(player.Slot);
        }

        public static void HideAll()
        {
            var hud = PanoramaBootstrap.TeamPowers;
            if (hud == null) return;

            foreach (var slot in _states.Keys.ToArray())
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player != null && player.IsValid) Hide(player, hud);
                else _states.Remove(slot);
            }
        }

        public static void ClearSlot(int slot) => _states.Remove(slot);

        public static void ResetRendered()
        {
            foreach (var state in _states.Values)
            {
                state.Signature = "";
                for (int i = 0; i < state.Tones.Length; i++) state.Tones[i] = -1;
            }
        }

        private static void Render(CCSPlayerController player, PanelHandle hud, State state)
        {
            var teammates = SkillUtils.CachedPlayers
                .Where(p => p.Slot != player.Slot
                         && p.Team == player.Team
                         && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist)
                .OrderBy(p => p.Slot)
                .Take(HudContract.TeamRows)
                .ToArray();

            var signature = string.Join('|', teammates.Select(p =>
            {
                var info = SkillUtils.GetPlayerInfo(p) ?? Event.EnsurePlayerInfo(p);
                return $"{info?.PlayerName}:{info?.Skill}:{p.PawnIsAlive}";
            }));

            if (signature == state.Signature) return;
            state.Signature = signature;

            for (int i = 0; i < HudContract.TeamRows; i++)
            {
                string rowId = $"SmTeamRow{i}";

                if (i >= teammates.Length)
                {
                    hud.SetClassFor(player, rowId, HudContract.HiddenClass, true);
                    ToggleTone(hud, player, i, ref state.Tones[i], -1);
                    continue;
                }

                var mate = teammates[i];
                var info = SkillUtils.GetPlayerInfo(mate) ?? Event.EnsurePlayerInfo(mate);
                var skill = info == null ? null : SkillData.Find(info.Skill);

                hud.SetClassFor(player, rowId, HudContract.HiddenClass, false);
                hud.SetClassFor(player, rowId, "sm-dead", !mate.PawnIsAlive);

                hud.SetVariableFor(player, $"sm_t{i}_name", info?.PlayerName ?? mate.PlayerName);
                hud.SetVariableFor(player, $"sm_t{i}_skill", skill?.Name ?? "—");
                ToggleTone(hud, player, i, ref state.Tones[i], skill?.Tone ?? -1);
            }
        }

        private static void ToggleTone(PanelHandle hud, CCSPlayerController player, int row, ref int current, int tone)
        {
            if (current == tone) return;

            string panelId = $"SmTeamSkill{row}";
            if (current >= 0) hud.SetClassFor(player, panelId, HudPalette.ClassFor(current), false);
            if (tone >= 0) hud.SetClassFor(player, panelId, HudPalette.ClassFor(tone), true);
            current = tone;
        }
    }
}
