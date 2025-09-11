using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class Command
    {
        private static bool gamePaused = false;
        private static readonly Config.Settings config = Config.LoadedConfig.Settings;

        public static void Load()
        {
            if (config == null || config == null) return;
            var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
            {
                { SplitCommands(config.SetSkillCommands.Alias), ("Set skill", Command_SetSkill) },
                { SplitCommands(config.SkillsListCommands.Alias), ("Delete all records", Command_SkillsListMenu) },
                { SplitCommands(config.UseSkillCommands.Alias), ("Use/Type skill", Command_UseTypeSkill) },
                { SplitCommands(config.ChangeMapCommands.Alias), ("Change map", Command_ChangeMap) },
                { SplitCommands(config.ConsoleCommands.Alias), ("Console command", Command_CustomCommand) },
                { SplitCommands(config.StartGameCommands.Alias), ("Start game", Command_StartGame) },
                { SplitCommands(config.SwapCommands.Alias), ("Swap team", Command_Swap) },
                { SplitCommands(config.ShuffleCommands.Alias), ("Shuffle team", Command_Shuffle) },
                { SplitCommands(config.PauseCommands.Alias), ("Pause game", Command_Pause) },
                { SplitCommands(config.HealCommands.Alias), ("Heal", Command_Heal) },
                { SplitCommands(config.SetScoreCommands.Alias), ("Set teams score", Command_SetScore) },
                { SplitCommands(config.SetStaticSkillCommands.Alias), ("Set static skill", Command_SetStaticSkill) },
            };

            foreach (var commandPair in commands)
                foreach (var command in commandPair.Key)
                    Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
        }

        private static IEnumerable<string> SplitCommands(string commands)
        {
            return commands.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_UseTypeSkill(CCSPlayerController? player, CommandInfo _)
        {
            if (player == null) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            string[] commands = _.ArgString.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteToDebug($"Player {player.PlayerName} used the skill: {playerInfo.Skill}");
            if (commands == null || commands.Length == 0)
                Instance.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
            else
                Instance.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [player, commands]);
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_SetSkill(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_setskill {command.ArgString} command.");
            if (player == null || !AdminManager.PlayerHasPermissions(player, config.SetSkillCommands.Permissions)) return;
            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => !p.IsBot
                                                                          && (p.SteamID.ToString().Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)
                                                                          || p.PlayerName.Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)) );

            if (command.ArgCount < 2)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("correct_form_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            if (targetPlayer == null)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("player_not_found_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            var skillName = command.ArgCount > 3 ? $"{command.GetArg(2)} {command.GetArg(3)}" : command.GetArg(2);
            var skill = SkillData.Skills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("skill_not_found_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == targetPlayer.SteamID);
            if (skillPlayer != null)
            {
                Instance.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [targetPlayer]);
                skillPlayer.Skill = skill.Skill;
                skillPlayer.SpecialSkill = src.player.Skills.None;
                Instance.SkillAction(skill.Skill.ToString(), "EnableSkill", [targetPlayer]);

                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, $"{Localization.GetTranslation("done_setskill")}: {ChatColors.LightRed}{skill.Name} {ChatColors.Lime}{Localization.GetTranslation("for_setskill")} {ChatColors.LightRed}{targetPlayer.PlayerName}", false);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
            }
            else
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("error_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
            }
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_SkillsListMenu(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_skills {command.ArgString} command.");
            if (player == null || !AdminManager.PlayerHasPermissions(player, config.SkillsListCommands.Permissions)) return;
            Menu.DisplaySkillsList(player);
        }

        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_ChangeMap(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_map {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.ChangeMapCommands.Permissions))
            {
                if (!config.ChangeMapCommands.EnableVoting) return;
                player.Vote(VoteType.ChangeMap, command.ArgString);
                return;
            }
            ChangeMap(command);
        }

        private static void ChangeMap(CommandInfo command)
        {
            string map = command.GetArg(1);

            if (string.IsNullOrEmpty(map))
            {
                command.ReplyToCommand($" {ChatColors.Red}{Localization.GetTranslation("invalid_map")}");
                return;
            }

            Server.PrintToChatAll($" {ChatColors.Yellow}{Localization.GetTranslation("loading_map")} ({ChatColors.Green}{map}{ChatColors.Yellow})...");

            if (uint.TryParse(map, out _))
                Server.ExecuteCommand($"host_workshop_map {map}");
            else if (!Server.IsMapValid(map))
                command.ReplyToCommand($" {ChatColors.Red}{Localization.GetTranslation("invalid_map")}");
            else
                Server.ExecuteCommand($"changelevel {map}");
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_StartGame(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_start {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.StartGameCommands.Permissions))
            {
                if (!config.StartGameCommands.EnableVoting) return;
                player.Vote(VoteType.StartGame);
                return;
            }
            StartGame(command);
        }

        private static void StartGame(CommandInfo command)
        {
            int cheats = command.GetArg(1) == "sv" ? 1 : 0;
            Server.ExecuteCommand($"mp_freezetime {(cheats == 1 ? 0 : 5)}");
            Server.ExecuteCommand("mp_warmup_end");
            Server.ExecuteCommand("mp_restartgame 1");

            Instance.AddTimer(2.0f, () => {
                Server.PrintToChatAll($" {ChatColors.Green}{Localization.GetTranslation("game_start")}");
                Server.ExecuteCommand("mp_forcecamera 0");
                Server.ExecuteCommand($"mp_freezetime {(cheats == 1 ? 0 : 5)}");
                Server.ExecuteCommand("mp_overtime_enable 1");
                Server.ExecuteCommand($"sv_cheats {cheats}");
            });
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Swap(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_swap {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.SwapCommands.Permissions))
            {
                if (!config.SwapCommands.EnableVoting) return;
                player.Vote(VoteType.SwapTeam);
                return;
            }
            Swap();
        }

        private static void Swap()
        {
            foreach (var player in Utilities.GetPlayers())
                if (Instance.IsPlayerValid(player) && new CsTeam[] { CsTeam.CounterTerrorist, CsTeam.Terrorist }.Contains(player.Team))
                    player.SwitchTeam(player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
            Server.ExecuteCommand($"mp_restartgame 1");
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Shuffle(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_shuffle {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.ShuffleCommands.Permissions))
            {
                if (!config.ShuffleCommands.EnableVoting) return;
                player.Vote(VoteType.ShuffleTeam);
                return;
            }
            Shuffle();
        }

        private static void Shuffle()
        {
            var players = Utilities.GetPlayers().FindAll(p => (Instance.IsPlayerValid(p) && new CsTeam[] { CsTeam.CounterTerrorist, CsTeam.Terrorist }.Contains(p.Team)));
            double CTlimit = Instance.Random.Next(0, 2) == 0 ? Math.Floor(players.Count / 2.0) : Math.Ceiling(players.Count / 2.0);

            foreach (var player in players.OrderBy(_ => Instance.Random.Next()).ToList())
            {
                player?.SwitchTeam(CTlimit > 0 ? CsTeam.CounterTerrorist : CsTeam.Terrorist);
                CTlimit--;
            }
            Server.ExecuteCommand($"mp_restartgame 1");
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Pause(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_pause {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.PauseCommands.Permissions))
            {
                if (!config.PauseCommands.EnableVoting) return;
                player.Vote(VoteType.PauseGame);
                return;
            }
            Pause();
        }

        private static void Pause()
        {
            Server.PrintToChatAll($" {(gamePaused ? ChatColors.Green : ChatColors.Red)}{Localization.GetTranslation(gamePaused ? "unpause" : "pause")}");
            Server.ExecuteCommand( gamePaused ? "mp_unpause_match" : "mp_pause_match");
            gamePaused = !gamePaused;
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_Heal(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_heal {command.ArgString} command.");
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!AdminManager.PlayerHasPermissions(player, config.HealCommands.Permissions)) return;
            SkillUtils.AddHealth(player.PlayerPawn.Value, 100);
            player.PrintToChat($" {ChatColors.Green}{Localization.GetTranslation("healed")}");
        }

        [CommandHelper(minArgs: 2, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_SetScore(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_setscore {command.ArgString} command.");
            if (player != null && player.IsValid && !AdminManager.PlayerHasPermissions(player, config.SetScoreCommands.Permissions))
            {
                if (!config.SetScoreCommands.EnableVoting) return;
                player.Vote(VoteType.SetScore, command.ArgString);
                return;
            }
            SetScore(player, command);
        }

        private static void SetScore(CCSPlayerController? player, CommandInfo command)
        {
            if (!int.TryParse(command.GetArg(1), out int ctScore) || !int.TryParse(command.GetArg(2), out int tScore))
            {
                if (player != null && player.IsValid)
                    SkillUtils.PrintToChat(player, Localization.GetTranslation("correct_form_setscore"), true);
                return;
            }

            SkillUtils.SetTeamScores((short)ctScore, (short)tScore, RoundEndReason.RoundDraw);
        }

        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        private static void Command_CustomCommand(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_console {command.ArgString} command.");
            if (player == null || !AdminManager.PlayerHasPermissions(player, config.ConsoleCommands.Permissions)) return;
            string param = command.ArgString;
            Server.ExecuteCommand(param);
        }

        [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private static void Command_SetStaticSkill(CCSPlayerController? player, CommandInfo command)
        {
            Debug.WriteToDebug($"Player {player?.PlayerPawn} used the css_setstaticskill {command.ArgString} command.");
            if (player == null || !AdminManager.PlayerHasPermissions(player, config.SetStaticSkillCommands.Permissions)) return;
            var targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => !p.IsBot
                                                                          && (p.SteamID.ToString().Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)
                                                                          || p.PlayerName.Equals(command.GetArg(1), StringComparison.CurrentCultureIgnoreCase)));

            if (command.ArgCount < 2)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("correct_form_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            if (targetPlayer == null)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("player_not_found_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            var skillName = command.ArgCount > 3 ? $"{command.GetArg(2)} {command.GetArg(3)}" : command.GetArg(2);
            var skill = SkillData.Skills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase) || s.Skill.ToString().Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null)
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("skill_not_found_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                return;
            }

            var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == targetPlayer.SteamID);
            if (skillPlayer != null)
            {
                Instance.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [targetPlayer]);
                skillPlayer.Skill = skill.Skill;
                skillPlayer.SpecialSkill = src.player.Skills.None;

                if (skill.Skill == src.player.Skills.None)
                    Event.staticSkills.Remove(targetPlayer.SteamID);
                else
                    Event.staticSkills.Add(targetPlayer.SteamID, skill);
                Instance.SkillAction(skill.Skill.ToString(), "EnableSkill", [targetPlayer]);

                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, $"{Localization.GetTranslation("done_setskill")}: {ChatColors.LightRed}{skill.Name} {ChatColors.Lime}{Localization.GetTranslation("for_setskill")} {ChatColors.LightRed}{targetPlayer.PlayerName}", false);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
            }
            else
            {
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
                SkillUtils.PrintToChat(player, Localization.GetTranslation("error_setskill"), true);
                player.PrintToChat($" {ChatColors.Green}―――――――――――{ChatColors.DarkRed}◥◣◆◢◤{ChatColors.Green}―――――――――――");
            }
        }
    }
}