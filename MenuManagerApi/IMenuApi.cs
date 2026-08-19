using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
namespace MenuManager
{
    public interface IMenuApi
    {
        public IMenu GetMenu(string title, Action<CCSPlayerController>? backAction = null, Action<CCSPlayerController>? resetAction = null);

        // Deprecated, only for backward compatibility
        [Obsolete("This method is kept only for backward compatibility.", true)]
        public IMenu NewMenu(string title, Action<CCSPlayerController>? backAction = null);
        //

        public IMenu GetMenuForcetype(string title, MenuType type, Action<CCSPlayerController>? backAction = null, Action<CCSPlayerController>? resetAction = null);

        // Deprecated, only for backward compatibility
        [Obsolete("This method is kept only for backward compatibility.", true)]
        public IMenu NewMenuForcetype(string title, MenuType type, Action<CCSPlayerController>? backAction = null);
        //

        public void CloseMenu(CCSPlayerController player);
        public MenuType GetMenuType(CCSPlayerController player);
        public bool HasOpenedMenu(CCSPlayerController player);
        public void SetMenuOptionFocusChangedHandler(IMenu menu, Action<CCSPlayerController, ChatMenuOption, int>? handler);
    }

    public enum MenuType
    {
        Default = -1,
        ChatMenu = 0,
        ConsoleMenu = 1,
        CenterMenu = 2,
        ButtonMenu = 3,
        MetamodMenu = 4
    }
}
