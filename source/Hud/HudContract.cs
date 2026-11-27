namespace Supermoce
{
    public static class HudContract
    {
        public const string Version = "v3";

        public const string LayoutRoot = "panorama/layout/custom_game/supermoce";

        public const string SkillHudPath = LayoutRoot + "/skill_hud_v3.vxml_c";
        public const string TeamPowersPath = LayoutRoot + "/team_powers_v3.vxml_c";
        public const string SkillMenuPath = LayoutRoot + "/skill_menu_v3.vxml_c";
        public const string SpectatorSkillPath = LayoutRoot + "/spectator_skill_v3.vxml_c";

        public const string SkillHudRoot = "SmHudRoot";
        public const string TeamPowersRoot = "SmTeamRoot";
        public const string SkillMenuRoot = "SmMenuRoot";
        public const string SpectatorSkillRoot = "SmSpecRoot";

        public const int TeamRows = 9;
        public const int MenuRows = 8;

        public const string HiddenClass = "sm-off";
        public const string DisabledClass = "sm-disabled";
        public const string ActiveClass = "sm-active";
        public const string ToastClass = "sm-toast";
    }
}
