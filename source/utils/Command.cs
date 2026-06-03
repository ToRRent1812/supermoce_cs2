using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public static class Command
    {
        public static void Load()
        {
            Instance?.AddCommand($"css_useskill", "Użyj mocy na dowolnym klawiszu", Command_UseTypeSkill);
            Instance?.AddCommand($"css_giveskill", "Admin: Nadaj sobie wybraną supermoc (css_giveskill supermoc)", Command_GiveSkill);
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_UseTypeSkill(CCSPlayerController? player, CommandInfo _)
        {
            if (player == null || Instance?.GameRules?.FreezePeriod == true) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null || playerInfo.IsDrawing) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            string[] commands = _.ArgString.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (commands == null || commands.Length == 0)
                Instance?.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
            else
                Instance?.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [player, commands]);
        }

        [CommandHelper(minArgs: 1, usage: "<NazwaSuperMocy>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [RequiresPermissions("@css/cheats")]
        private static void Command_GiveSkill(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || Instance?.GameRules == null) return;
            if (!AdminManager.PlayerHasPermissions(player, "@css/cheats"))
            {
                SkillUtils.PrintToChat(player, "Brak uprawnień administracyjnych!", true);
                return;
            }

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            string skillName = command.GetArg(1);

            if (!Enum.TryParse<Skills>(skillName, true, out var targetSkill))
            {
                SkillUtils.PrintToChat(player, $"Nie znaleziono supermocy o nazwie '{skillName}'!", true);

                var availableSkills = SkillData.Skills
                    .Where(s => s.Skill != Skills.None)
                    .Select(s => s.Skill.ToString())
                    .OrderBy(s => s);

                SkillUtils.PrintToChat(player, $"Dostępne: {string.Join(", ", availableSkills)}", false);
                return;
            }

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == targetSkill);
            if (skillData == null)
            {
                SkillUtils.PrintToChat(player, $"Supermoc '{targetSkill}' nie jest zarejestrowana!", true);
                return;
            }

            if (playerInfo.Skill != Skills.None)
            {
                Instance?.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [player]);
                Instance?.SkillAction(playerInfo.Skill.ToString(), "NewRound");
            }

            playerInfo.Skill = targetSkill;
            playerInfo.SpecialSkill = Skills.None;
            playerInfo.IsDrawing = false;
            Instance?.SkillAction(targetSkill.ToString(), "EnableSkill", [player]);

            SkillUtils.PrintToChat(player, $"Otrzymałeś supermoc: {skillData.Name}");
        }
    }
}
