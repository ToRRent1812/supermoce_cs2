using CounterStrikeSharp.API.Core;
using PanoramaManager;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Supermoce
{
    public static class PanoramaBootstrap
    {
        public static bool Ready { get; private set; }

        public static PanelHandle? SkillHud { get; private set; }
        public static PanelHandle? TeamPowers { get; private set; }
        public static PanelHandle? SkillMenu { get; private set; }
        public static PanelHandle? SpectatorSkill { get; private set; }

        public static void Init(BasePlugin plugin)
        {
            try
            {
                Panorama.Init(plugin);

                SkillHud = Panorama.Spawn(HudContract.SkillHudPath, new LayoutContract
                {
                    RootPanelId = HudContract.SkillHudRoot,
                    RowCount = 0,
                    CaptureInput = false,
                    HideFromSpectators = false,
                    HiddenClass = HudContract.HiddenClass,
                });

                TeamPowers = Panorama.Spawn(HudContract.TeamPowersPath, new LayoutContract
                {
                    RootPanelId = HudContract.TeamPowersRoot,
                    RowCount = 0,
                    CaptureInput = false,
                    HideFromSpectators = false,
                    HiddenClass = HudContract.HiddenClass,
                });

                SkillMenu = Panorama.Spawn(HudContract.SkillMenuPath, new LayoutContract
                {
                    RootPanelId = HudContract.SkillMenuRoot,
                    RowCount = HudContract.MenuRows,
                    CaptureInput = true,
                    HideFromSpectators = true,
                    HiddenClass = HudContract.HiddenClass,
                    DisabledClass = HudContract.DisabledClass,
                    ActiveClass = HudContract.ActiveClass,
                    CloseButtonId = "menu_close",
                    PrevButtonId = "nav_prev",
                    NextButtonId = "nav_next",
                });

                SpectatorSkill = Panorama.Spawn(HudContract.SpectatorSkillPath, new LayoutContract
                {
                    RootPanelId = HudContract.SpectatorSkillRoot,
                    RowCount = 0,
                    CaptureInput = false,
                    HideFromSpectators = true,
                    HiddenClass = HudContract.HiddenClass,
                });

                plugin.RegisterListener<OnClientDisconnect>(OnClientDisconnect);

                SkillHud!.OnEvent += e =>
                {
                    if (e.Action == PanelAction.Restored) SkillHudPanel.ClearCache();
                };
                TeamPowers!.OnEvent += e =>
                {
                    if (e.Action == PanelAction.Restored) TeamPowersPanel.ResetRendered();
                };
                SkillMenu!.OnEvent += SkillMenuPanel.OnEvent;
                SpectatorSkill!.OnEvent += e =>
                {
                    if (e.Action == PanelAction.Restored)
                    {
                        SpectatorSkillPanel.ClearAll();
                        ToastService.ClearAll();
                    }
                };

                Ready = true;
                plugin.Logger.LogInformation("[Supermoce] Panorama registered (HUD {Version}).", HudContract.Version);
            }
            catch (Exception ex)
            {
                Ready = false;
                plugin.Logger.LogError(ex, "[Supermoce] Panorama init failed");
            }
        }

        private static void OnClientDisconnect(int slot)
        {
            if (SkillHud != null) SkillHud.Close(slot);
            if (TeamPowers != null) TeamPowers.Close(slot);
            if (SkillMenu != null) SkillMenu.Close(slot);
            if (SpectatorSkill != null) SpectatorSkill.Close(slot);
            SkillHudPanel.ClearSlot(slot);
            TeamPowersPanel.ClearSlot(slot);
            ToastService.ClearSlot(slot);
            SkillMenuPanel.ClearSlot(slot);
            SpectatorSkillPanel.ClearSlot(slot);
        }

        public static void Shutdown()
        {
            SkillHud?.Dispose();
            TeamPowers?.Dispose();
            SkillMenu?.Dispose();
            SpectatorSkill?.Dispose();

            SkillHud = null;
            TeamPowers = null;
            SkillMenu = null;
            SpectatorSkill = null;

            Panorama.Shutdown();
            Ready = false;
        }
    }
}
