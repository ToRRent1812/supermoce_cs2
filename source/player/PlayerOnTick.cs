using static CounterStrikeSharp.API.Core.Listeners;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public static class PlayerOnTick
    {
        public static void Load()
        {
            Instance?.RegisterListener<OnTick>(() =>
            {
                SkillUtils.RefreshTickState();
                GameState.Refresh();
                Bomb.Refresh();
                SkillHudPanel.Tick();
                ToastService.Tick();
                SpectatorSkillPanel.Tick();
                TeamPowersPanel.Tick();
            });

            Instance?.RegisterListener<OnMapStart>(OnMapStart);
        }

        private static void OnMapStart(string mapName)
        {
            if (Instance != null)
                Instance.GameRules = null;
            TeamPowersPanel.HideAll();
            ToastService.ClearAll();
            SkillMenuPanel.ClearAll();
            SpectatorSkillPanel.ClearAll();
            SkillHudPanel.ClearCache();
        }
    }
}
