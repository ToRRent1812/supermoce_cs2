using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using WASDMenuAPI.Classes;
using WASDSharedAPI;

namespace WASDMenuAPI;

public class WasdMenuPlayer
{
    public required CCSPlayerController Player { get; set; }
    public WasdMenu? MainMenu = null;
    public LinkedListNode<IWasdMenuOption>? CurrentChoice = null;
    public LinkedListNode<IWasdMenuOption>? MenuStart = null;
    public string CenterHtml = "";
    public int VisibleOptions = 4;
    public PlayerButtons Buttons { get; set; }

    public int scrollIndex = 0;
    public int maxLength = 21;
    public int scrollJump = 3;

    public void OpenMainMenu(WasdMenu? menu)
    {
        if (menu == null)
        {
            MainMenu = null;
            CurrentChoice = null;
            CenterHtml = "";
            return;
        }
        MainMenu = menu;
        CurrentChoice = MainMenu.Options?.First;
        MenuStart = CurrentChoice;
        UpdateCenterHtml();
    }

    public void OpenSubMenu(IWasdMenu? menu)
    {
        if (menu == null)
        {
            CurrentChoice = MainMenu?.Options?.First;
            MenuStart = CurrentChoice;
            UpdateCenterHtml();
            return;
        }

        CurrentChoice = menu.Options?.First;
        MenuStart = CurrentChoice;
        UpdateCenterHtml();
    }
    public void GoBackToPrev(LinkedListNode<IWasdMenuOption>? menu)
    {
        if (menu == null)
        {
            CurrentChoice = MainMenu?.Options?.First;
            MenuStart = CurrentChoice;
            UpdateCenterHtml();
            return;
        }

        CurrentChoice = menu;
        if (CurrentChoice.Value.Index >= 5 )
        {
            MenuStart = CurrentChoice;
            for (int i = 0; i < VisibleOptions; i++)
            {
                MenuStart = MenuStart?.Previous;
            }
        }
        else
            MenuStart = CurrentChoice.List?.First;
        UpdateCenterHtml();
    }

    public void CloseSubMenu()
    {
        if (CurrentChoice?.Value.Parent?.Prev == null)
            return;
        GoBackToPrev(CurrentChoice?.Value.Parent.Prev);
    }

    public void CloseAllSubMenus()
    {
        OpenSubMenu(null);
    }
    
    public void Choose()
    {
        CurrentChoice?.Value.OnChoose?.Invoke(Player, CurrentChoice.Value);
    }

    public void ScrollDown()
    {
        if(CurrentChoice == null || MainMenu == null)
            return;
        scrollIndex = 0;
        CurrentChoice = CurrentChoice.Next ?? CurrentChoice.List?.First;
        MenuStart = CurrentChoice!.Value.Index >= VisibleOptions ? MenuStart!.Next : CurrentChoice.List?.First;
        UpdateCenterHtml();
    }
    
    public void ScrollUp()
    {
        if(CurrentChoice == null || MainMenu == null)
            return;
        scrollIndex = 0;
        CurrentChoice = CurrentChoice.Previous ?? CurrentChoice.List?.Last;
        if (CurrentChoice == CurrentChoice?.List?.Last && CurrentChoice?.Value.Index >= VisibleOptions)
        {
            MenuStart = CurrentChoice;
            for (int i = 0; i < VisibleOptions - 1; i++)
                MenuStart = MenuStart?.Previous;
        }
        else
            MenuStart = CurrentChoice!.Value.Index >= VisibleOptions ? MenuStart!.Previous : CurrentChoice.List?.First;
        UpdateCenterHtml();
    }

    private void UpdateCenterHtml()
    {
        if (CurrentChoice == null || MainMenu == null || MenuStart == null)
            return;

        StringBuilder builder = new();
        string itemText = "";
        string itemHoverText = "";
        string endControl = "";
        LinkedListNode<IWasdMenuOption>? option = MenuStart;

        if (!string.IsNullOrEmpty(option?.Value?.Parent?.Title))
            builder.AppendLine(option.Value.Parent?.Title ?? "");

        if (!string.IsNullOrEmpty(option?.Value?.Parent?.ItemText))
            itemText = option?.Value?.Parent?.ItemText ?? "";

        if (!string.IsNullOrEmpty(option?.Value?.Parent?.ItemHoverText))
            itemHoverText = option?.Value?.Parent?.ItemHoverText ?? "";

        if (!string.IsNullOrEmpty(option?.Value?.Parent?.ControlText))
            endControl = option?.Value?.Parent?.ControlText ?? "";

        int shown = 0;
        int maxOptions = VisibleOptions;
        
        while (shown < maxOptions && option != null && option.Value != null)
        {
            /*            if (shown == maxOptions - 1 && option.Next != null)
                        {
                            builder.AppendLine(
                                $"<img src='https://raw.githubusercontent.com/oqyh/cs2-Kill-Sound-GoldKingZ/main/Resources/arrow.gif' class=''> <img src='https://raw.githubusercontent.com/oqyh/cs2-Kill-Sound-GoldKingZ/main/Resources/arrow.gif' class=''> <img src='https://raw.githubusercontent.com/oqyh/cs2-Kill-Sound-GoldKingZ/main/Resources/arrow.gif' class=''> <br>");
                            break;
                        }*/

            string input = option.Value.OptionDisplay ?? string.Empty;
            string color = string.Empty;
            string text = input;

            if (input.Contains('|'))
            {
                string[] parts = input.Split("|", 2);
                color = parts[0].StartsWith("#") && parts[0].Length == 7 ? parts[0] : string.Empty;
                text = parts.Length > 1 ? parts[1] : "";
            }
            
            if (option == CurrentChoice)
            {
                if (text.Length > maxLength)
                {
                    int remaining = text.Length - scrollIndex;
                    if (remaining <= maxLength)
                    {
                        text = SafeSubstring(text, scrollIndex, remaining);
                        if (Server.TickCount % 32 == 0)
                            scrollIndex = 0;
                    }
                    else
                    {
                        text = SafeSubstring(text, scrollIndex, maxLength);
                        if (Server.TickCount % 32 == 0)
                            scrollIndex += scrollJump;
                    }
                }
                else
                    text = SafeSubstring(text, 0, maxLength);

                builder.AppendLine(string.Format(itemHoverText, text));
            }
            else
                builder.AppendLine(string.Format(itemText, $"<font {(string.IsNullOrEmpty(color) ? "" : $"color='{color}'")}'>{SafeSubstring(text, 0, maxLength)}</font>"));

            option = option.Next;
            shown++;
        }

        if (!string.IsNullOrEmpty(endControl))
            builder.AppendLine(endControl);

        CenterHtml = builder.ToString();
    }

    public void UpdateActiveMenu(Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> list)
    {
        if (list.Count == 0)
        {
            OpenMainMenu(null);
            return;
        }

        if (MainMenu == null)
            return;

        var options = MainMenu.Options;
        if (options == null)
            return;

        options.Clear();
        bool choiceExists = false;

        foreach (var item in list)
        {
            WasdMenuOption newOption = new()
            {
                OptionDisplay = item.Key,
                OnChoose = item.Value,
                Index = options.Count,
                Parent = MainMenu
            };
            options.AddLast(newOption);

            if (CurrentChoice?.Value?.OptionDisplay == item.Key)
            {
                CurrentChoice = options.Last;
                choiceExists = true;
            }
        }
        if (!choiceExists)
            CurrentChoice = options.First;

        if (CurrentChoice != null)
        {
            if (CurrentChoice.Value.Index >= VisibleOptions)
            {
                MenuStart = CurrentChoice;
                for (int i = 0; i < VisibleOptions - 1; i++)
                    MenuStart = MenuStart?.Previous;
            }
            else
                MenuStart = MainMenu.Options?.First;
        }

        UpdateCenterHtml();
    }

    private static string SafeSubstring(string text, int start, int length)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (start < 0) start = 0;
        if (start >= text.Length) return "";
        if (start + length > text.Length)
            length = text.Length - start;
        return text.Substring(start, length);
    }
}