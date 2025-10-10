using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills;

public class FindTarget
{
    public static (List<CCSPlayerController> players, string targetname) Find
        (
            CommandInfo command,
            int minArgCount,
            bool singletarget,
            bool ignoreMessage = false
        )
    {
        if (command.ArgCount < minArgCount)
            return ([], string.Empty);

        var targetresult = command.GetArgTargetResult(1);

        if (targetresult.Players.Count == 0)
        {
            if (!ignoreMessage && command.CallingPlayer != null)
                command.ReplyToCommand("Brak pasującego gracza");
            return ([], string.Empty);
        }

        if (singletarget && targetresult.Players.Count > 1)
        {
            if (command.CallingPlayer != null)
                command.ReplyToCommand("Znaleziono więcej niż 1 gracza o tej samej nazwie.");
            return ([], string.Empty);
        }

        if (targetresult.Players.Count == 1)
            return (targetresult.Players, targetresult.Players.Single().PlayerName);

        Target.TargetTypeMap.TryGetValue(command.GetArg(1), out TargetType type);

        string targetname = type switch
        {
            TargetType.GroupAll => Instance?.Localizer["all"] ?? "all",
            TargetType.GroupBots => Instance?.Localizer["bots"] ?? "bots",
            TargetType.GroupHumans => Instance?.Localizer["humans"] ?? "humans",
            TargetType.GroupAlive => Instance?.Localizer["alive"] ?? "alive",
            TargetType.GroupDead => Instance?.Localizer["dead"] ?? "dead",
            TargetType.GroupNotMe => Instance?.Localizer["notme"] ?? "notme",
            TargetType.PlayerMe => targetresult.Players.First().PlayerName,
            TargetType.TeamCt => Instance?.Localizer["ct"] ?? "ct",
            TargetType.TeamT => Instance?.Localizer["t"] ?? "t",
            TargetType.TeamSpec => Instance?.Localizer["spec"] ?? "spec",
            _ => targetresult.Players.First().PlayerName
        };

        return (targetresult.Players, targetname);
    }
}