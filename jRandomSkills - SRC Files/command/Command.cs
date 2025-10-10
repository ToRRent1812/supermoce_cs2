using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class Command
    {
        public static void Load()
        {
            Instance?.AddCommand($"css_useskill", "Use/Type skill", Command_UseTypeSkill);
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_UseTypeSkill(CCSPlayerController? player, CommandInfo _)
        {
            if (player == null || Instance?.GameRules?.FreezePeriod == true) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null || playerInfo.IsDrawing) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            string[] commands = _.ArgString.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteToDebug($"{player.PlayerName} użył {playerInfo.Skill}");
            if (commands == null || commands.Length == 0)
                Instance?.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
            else
                Instance?.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [player, commands]);
        }
    }
}