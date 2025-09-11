using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using System.Text.RegularExpressions;
using static jRandomSkills.Config;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static partial class Event
    {
        private static readonly jSkill_SkillInfo noneSkill = new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false);

        private static jSkill_SkillInfo ctSkill = noneSkill;
        private static jSkill_SkillInfo tSkill = noneSkill;
        private static jSkill_SkillInfo allSkill = noneSkill;
        private static List<jSkill_SkillInfo> debugSkills = new(SkillData.Skills);

        private static readonly Dictionary<ulong, List<jSkill_SkillInfo>> playersSkills = [];
        public static readonly Dictionary<ulong, jSkill_SkillInfo> staticSkills = [];

        public static void Load()
        {
            Instance.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !player.IsValid) return HookResult.Continue;

                Instance.SkillPlayer.Add(new jSkill_PlayerInfo
                {
                    SteamID = player.SteamID,
                    PlayerName = player.PlayerName,
                    Skill = Skills.None,
                    SpecialSkill = Skills.None,
                    IsDrawing = false,
                    SkillChance = 1,
                    RandomPercentage = "",
                });

                player.PrintToChat($" {ChatColors.DarkRed}UWAGA!{ChatColors.Green} By użyć niektórych supermocy, musisz zbindować sobie klawisz.");
                player.PrintToChat($" {ChatColors.Green}By zapisać komendę na stałe, musisz wyjść z serwera i wpisać komendę w menu głównym.");
                player.PrintToChat($" {ChatColors.Green}Żeby ustawić np. klawisz V, wpisz w konsolę {ChatColors.Yellow}bind v css_useskill");

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !player.IsValid) return HookResult.Continue;

                var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (skillPlayer != null)
                {
                    Instance.SkillPlayer.Remove(skillPlayer);
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (skillPlayer != null && !SkillUtils.IsWarmup())
                    {
                        skillPlayer.IsDrawing = true;
                        skillPlayer.RandomPercentage = "";
                    }
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    Instance.AddTimer(0.5f, () =>
                    {
                        var _players = Utilities.GetPlayers().Where(p => p.IsValid).OrderBy(p => p.Team);

                        string skillsText = "";
                        foreach (var _player in _players)
                        {
                            var _playerSkill = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == _player.SteamID);
                            if (_playerSkill != null)
                            {
                                var skillInfo = SkillData.Skills.FirstOrDefault(p => p.Skill == _playerSkill.Skill);
                                var specialSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == _playerSkill.SpecialSkill);
                                skillsText += $" {ChatColors.DarkRed}{_player.PlayerName}{ChatColors.Lime}: {(_playerSkill.SpecialSkill == Skills.None ? skillInfo.Name : $"{specialSkillInfo.Name} -> {skillInfo.Name}")}\n";
                                _playerSkill.RandomPercentage = "";
                            }
                        }

                        if (Config.LoadedConfig.Settings.SummaryAfterTheRound && !string.IsNullOrEmpty(skillsText))
                        {
                            player.PrintToChat(" ");
                            player.PrintToChat($" {ChatColors.Lime}{Localization.GetTranslation("summary_start")}");
                            foreach (string text in skillsText.Split("\n"))
                                if (!string.IsNullOrEmpty(text))
                                    player.PrintToChat(text);
                            player.PrintToChat($" {ChatColors.Lime}{Localization.GetTranslation("summary_end")}");
                            player.PrintToChat(" \n");
                        }
                    });
                }

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Config.DefaultSkillInfo[] terroristSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == (int)CsTeam.Terrorist).ToArray();
                Config.DefaultSkillInfo[] counterterroristSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == (int)CsTeam.CounterTerrorist).ToArray();
                Config.DefaultSkillInfo[] allTeamsSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == 0).ToArray();

                if (Config.LoadedConfig.Settings.GameMode == (int)Config.GameModes.TeamSkills)
                {
                    List<jSkill_SkillInfo> tSkills = new(SkillData.Skills);
                    tSkills.RemoveAll(s => s.Skill == tSkill.Skill || s.Skill == Skills.None || counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    tSkill = tSkills.Count == 0 ? new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false) : tSkills[Instance.Random.Next(tSkills.Count)];

                    List<jSkill_SkillInfo> ctSkills = new(SkillData.Skills);
                    ctSkills.RemoveAll(s => s.Skill == ctSkill.Skill || s.Skill == Skills.None || terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    ctSkill = ctSkills.Count == 0 ? new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false) : ctSkills[Instance.Random.Next(ctSkills.Count)];
                }
                else if (Config.LoadedConfig.Settings.GameMode == (int)Config.GameModes.SameSkills)
                {
                    List<jSkill_SkillInfo> allSkills = new(SkillData.Skills);
                    allSkills.RemoveAll(s => s.Skill == allSkill.Skill || s.Skill == Skills.None || !allTeamsSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                    allSkill = allSkills.Count == 0 ? new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false) : allSkills[Instance.Random.Next(allSkills.Count)];
                }
                else if (Config.LoadedConfig.Settings.GameMode == (int)Config.GameModes.Debug && debugSkills.Count == 0)
                    debugSkills = new(SkillData.Skills);

                foreach (var player in Utilities.GetPlayers())
                {
                    var playerTeam = player.Team;
                    var teammates = Utilities.GetPlayers().Where(p => p.Team == playerTeam && p != player);
                    string teammateSkills = "";

                    var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                    if (skillPlayer != null && !SkillUtils.IsWarmup())
                    {
                        skillPlayer.IsDrawing = false;
                        jSkill_SkillInfo randomSkill = new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false);

                        Config.GameModes gameMode = (Config.GameModes)Config.LoadedConfig.Settings.GameMode;
                        if (staticSkills.TryGetValue(player.SteamID, out var staticSkill))
                            randomSkill = staticSkill;
                        else if (gameMode == Config.GameModes.Normal || gameMode == Config.GameModes.NoRepeat)
                        {
                            List<jSkill_SkillInfo> skillList = new(SkillData.Skills);
                            skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

                            if (Utilities.GetPlayers().FindAll(p => p.Team == player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator).Count == 1)
                            {
                                Config.DefaultSkillInfo[] skillsNeedsTeammates = Config.LoadedConfig.SkillsInfo.Where(s => s.NeedsTeammates).ToArray();
                                skillList.RemoveAll(s => skillsNeedsTeammates.Any(s2 => s2.Name == s.Skill.ToString()));
                            }

                            if (player.Team == CsTeam.Terrorist)
                                skillList.RemoveAll(s => counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                            else
                                skillList.RemoveAll(s => terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));

                            if (gameMode == Config.GameModes.NoRepeat && playersSkills.TryGetValue(player.SteamID, out List<jSkill_SkillInfo>? skills))
                            {
                                skillList.RemoveAll(s => skills.Any(s2 => s2.Skill == s.Skill));
                                if (skillList.Count == 0) skills.Clear();
                            }

                            randomSkill = skillList.Count == 0 ? noneSkill : skillList[Instance.Random.Next(skillList.Count)];
                            if (gameMode == Config.GameModes.NoRepeat)
                            {
                                if (playersSkills.TryGetValue(player.SteamID, out List<jSkill_SkillInfo>? value))
                                    value.Add(randomSkill);
                                else
                                    playersSkills.Add(player.SteamID, [randomSkill]);
                            }
                        }
                        else if (gameMode == Config.GameModes.TeamSkills)
                            randomSkill = player.Team == CsTeam.Terrorist ? tSkill : ctSkill;
                        else if (gameMode == Config.GameModes.SameSkills)
                            randomSkill = allSkill;
                        else if (gameMode == Config.GameModes.Debug)
                        {
                            if (debugSkills.Count == 0)
                                debugSkills = new List<jSkill_SkillInfo>(SkillData.Skills);
                            randomSkill = debugSkills[0];
                            debugSkills.RemoveAt(0);
                            player.PrintToChat($"{SkillData.Skills.Count - debugSkills.Count}/{SkillData.Skills.Count}");
                        }

                        skillPlayer.Skill = randomSkill.Skill;
                        skillPlayer.SpecialSkill = Skills.None;
                        Debug.WriteToDebug($"Player {skillPlayer.PlayerName} has got the skill \"{randomSkill.Name}\".");

                        /*if (randomSkill.Display)
                            SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{randomSkill.Name}{ChatColors.Lime}: {randomSkill.Description}", false);*/

                        if (Config.LoadedConfig.Settings.TeamMateSkillInfo)
                        {
                            Instance.AddTimer(0.3f, () =>
                            {
                                foreach (var teammate in teammates)
                                {
                                    var teammateSkill = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == teammate.SteamID)?.Skill;
                                    if (teammateSkill != null)
                                    {
                                        var skillInfo = SkillData.Skills.FirstOrDefault(p => p.Skill == teammateSkill);
                                        teammateSkills += $" {ChatColors.DarkRed}{teammate.PlayerName}{ChatColors.Green} - {(skillInfo == null ? Skills.None : skillInfo.Name)} {ChatColors.White}| ";
                                    }
                                }

                                if (!string.IsNullOrEmpty(teammateSkills))
                                {
                                    SkillUtils.PrintToChat(player, $" {ChatColors.Lime}{Localization.GetTranslation("teammate_skills")}:", false);
                                    foreach (string text in teammateSkills.Split("\n"))
                                        if (!string.IsNullOrEmpty(text))
                                            player.PrintToChat(text);
                                }
                            });
                        }
                    }
                }

                return HookResult.Continue;
            });


            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var victim = @event.Userid;
                var attacker = @event.Attacker;

                if (victim == null || attacker == null || victim == attacker) return HookResult.Continue;

                if (Config.LoadedConfig.Settings.KillerSkillInfo && !SkillUtils.IsWarmup())
                {
                    var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
                    if (attackerInfo != null)
                    {
                        var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.Skill);
                        var specialSkillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.SpecialSkill);
                        if (skillData == null || specialSkillData == null) return HookResult.Continue;
                        string skillDesc = skillData.Description;

                        SkillUtils.PrintToChat(victim, $"{Localization.GetTranslation("enemy_skill")} {ChatColors.DarkRed}{attacker.PlayerName}{ChatColors.Lime}:", false);
                        SkillUtils.PrintToChat(victim, $"{ChatColors.DarkRed}{(attackerInfo.SpecialSkill == Skills.None ? skillData.Name : $"{specialSkillData.Name} -> {skillData.Name}")}{ChatColors.Lime} - {skillDesc}", false);
                    }
                }

                var victimInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
                if (victimInfo != null)
                {
                    victimInfo.RandomPercentage = "";
                }

                return HookResult.Continue;
            });
        }
        [GeneratedRegex(@"\{AUTHOR1\}", RegexOptions.IgnoreCase, "pl-PL")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"\{AUTHOR2\}", RegexOptions.IgnoreCase, "pl-PL")]
        private static partial Regex MyRegex1();
    }
}
