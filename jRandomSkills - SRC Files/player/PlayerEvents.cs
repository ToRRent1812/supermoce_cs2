using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using System.Text.RegularExpressions;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static partial class Event
    {
        public static bool isTransmitRegistered = false;
        public static readonly jSkill_SkillInfo noneSkill = new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false);

        private static jSkill_SkillInfo ctSkill = noneSkill;
        private static jSkill_SkillInfo tSkill = noneSkill;
        private static jSkill_SkillInfo allSkill = noneSkill;
        private static List<jSkill_SkillInfo> debugSkills = new(SkillData.Skills);

        public static readonly Config.DefaultSkillInfo[] terroristSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == (int)CsTeam.Terrorist).ToArray();
        public static readonly Config.DefaultSkillInfo[] counterterroristSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == (int)CsTeam.CounterTerrorist).ToArray();
        private static readonly Config.DefaultSkillInfo[] allTeamsSkills = Config.LoadedConfig.SkillsInfo.Where(s => s.OnlyTeam == 0).ToArray();

        private static readonly Dictionary<ulong, List<jSkill_SkillInfo>> playersSkills = [];
        public static readonly Dictionary<ulong, jSkill_SkillInfo> staticSkills = [];

        public static void Load()
        {
            Instance.RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFull);
            Instance.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnect);
            Instance.RegisterEventHandler<EventRoundStart>(RoundStart);
            Instance.RegisterEventHandler<EventRoundEnd>(RoundEnd);
            
            Instance.RegisterEventHandler<EventPlayerDeath>(PlayerDeath);
            Instance.RegisterEventHandler<EventPlayerBlind>(PlayerBlind);
            Instance.RegisterEventHandler<EventPlayerHurt>(PlayerHurt);
            Instance.RegisterEventHandler<EventPlayerJump>(PlayerJump);

            Instance.RegisterEventHandler<EventWeaponFire>(WeaponFire);
            Instance.RegisterEventHandler<EventItemEquip>(WeaponEquip);
            Instance.RegisterEventHandler<EventItemPickup>(WeaponPickup);
            Instance.RegisterEventHandler<EventWeaponReload>(WeaponReload);
            Instance.RegisterEventHandler<EventGrenadeThrown>(GrenadeThrown);

            Instance.RegisterEventHandler<EventBombBeginplant>(BombBeginplant);
            Instance.RegisterEventHandler<EventBombPlanted>(BombPlanted);
            Instance.RegisterEventHandler<EventBombBegindefuse>(BombBegindefuse);

            Instance.RegisterEventHandler<EventDecoyStarted>(DecoyStarted);
            Instance.RegisterEventHandler<EventDecoyDetonate>(DecoyDetonate);

            Instance.RegisterEventHandler<EventSmokegrenadeDetonate>(SmokegrenadeDetonate);
            Instance.RegisterEventHandler<EventSmokegrenadeExpired>(SmokegrenadeExpired);

            Instance.RegisterListener<OnPlayerButtonsChanged>(CheckUseSkill);
            Instance.RegisterListener<OnEntitySpawned>(EntitySpawned);
            Instance.RegisterListener<OnTick>(OnTick);

            Instance.HookUserMessage(208, PlayerMakeSound);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        private static HookResult PlayerMakeSound(UserMessage um)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerMakeSound", [um]);
            return HookResult.Continue;
        }

        private static HookResult WeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "WeaponFire", [@event]);
            return HookResult.Continue;
        }

        private static HookResult WeaponEquip(EventItemEquip @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "WeaponEquip", [@event]);
            return HookResult.Continue;
        }

        private static HookResult WeaponPickup(EventItemPickup @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "WeaponPickup", [@event]);
            return HookResult.Continue;
        }

        private static HookResult WeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "WeaponReload", [@event]);
            return HookResult.Continue;
        }

        private static HookResult GrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "GrenadeThrown", [@event]);
            return HookResult.Continue;
        }

        private static HookResult BombBeginplant(EventBombBeginplant @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "BombBeginplant", [@event]);
            return HookResult.Continue;
        }

        private static HookResult BombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "BombPlanted", [@event]);
            return HookResult.Continue;
        }

        private static HookResult BombBegindefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "BombBegindefuse", [@event]);
            return HookResult.Continue;
        }

        private static HookResult DecoyStarted(EventDecoyStarted @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "DecoyStarted", [@event]);
            return HookResult.Continue;
        }

        private static HookResult DecoyDetonate(EventDecoyDetonate @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "DecoyDetonate", [@event]);
            return HookResult.Continue;
        }

        private static HookResult SmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "SmokegrenadeDetonate", [@event]);
            return HookResult.Continue;
        }

        private static HookResult SmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "SmokegrenadeExpired", [@event]);
            return HookResult.Continue;
        }

        private static HookResult PlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerHurt", [@event]);
            return HookResult.Continue;
        }

        private static HookResult PlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerJump", [@event]);
            return HookResult.Continue;
        }

        private static HookResult PlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerBlind", [@event]);
            return HookResult.Continue;
        }

        private static HookResult OnTakeDamage(DynamicHook h)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "OnTakeDamage", [h]);
            return HookResult.Continue;
        }

        private static void OnTick()
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "OnTick");

            /*foreach (var playerSkill in Instance.SkillPlayer)
            {
                if (playerSkill.SkillDescriptionHudExpired >= DateTime.Now)
                    if (playerSkill.Skill != Skills.None)
                        ShowSkillDescription(Utilities.GetPlayerFromSteamId(playerSkill.SteamID));
            }*/
        }

        private static HookResult PlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
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

            player.PrintToChat($" {ChatColors.DarkRed}UWAGA!{ChatColors.Green} By użyć niektórych supermocy, musisz wcisnąć klawisz inspekcji broni lub zrobić dodatkowy bind.");
            player.PrintToChat($" {ChatColors.Green}By zapisać własny klawisz na stałe, musisz wyjść z serwera i wpisać komendę {ChatColors.Yellow}bind v css_useskill{ChatColors.Green} w menu głównym.");

            /*const string defaultWelcomeMsg = "Welcome {PLAYER}, to the {SERVER_NAME} server!\n"
                + "The current version of the jRandomSkills: {VERSION} ({SKILLS_COUNT} skills).\n\n"
                + "Original plugin created by:\n{AUTHOR1}\n"
                + "Modified and improved by:\n{AUTHOR2}";
            string langWelcomeMsg = Localization.GetTranslation("welcome_message", "welcome");
            string welcomeMsg = MyRegex().IsMatch(langWelcomeMsg) && MyRegex1().IsMatch(langWelcomeMsg) ? langWelcomeMsg : defaultWelcomeMsg;

            foreach (string line in welcomeMsg.Split("\n"))
                player.PrintToChat($" {ChatColors.Green}" + line.Replace("{PLAYER}", $" {ChatColors.Red}{player.PlayerName}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                        .Replace("{SERVER_NAME}", $" {ChatColors.Red}{ConVar.Find("hostname")?.StringValue ?? "Default Server"}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                        .Replace("{VERSION}", $" {ChatColors.Red}v{Instance.ModuleVersion}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                        .Replace("{SKILLS_COUNT}", $" {ChatColors.Red}{SkillData.Skills.Count - 1}{ChatColors.Green}", StringComparison.OrdinalIgnoreCase)
                                        .Replace("{AUTHOR1}", $" {ChatColors.Red}Jakub Bartosik (D3X){ChatColors.Green} ({ChatColors.Red}https://github.com/jakubbartosik/dRandomSkills{ChatColors.Green})", StringComparison.OrdinalIgnoreCase)
                                        .Replace("{AUTHOR2}", $" {ChatColors.Red}Juzlus{ChatColors.Green} ({ChatColors.Red}https://github.com/Juzlus/jRandomSkills{ChatColors.Green})", StringComparison.OrdinalIgnoreCase));
*/
            return HookResult.Continue;
        }

        private static HookResult PlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return HookResult.Continue;

            var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (skillPlayer != null)
                Instance.SkillPlayer.Remove(skillPlayer);
            return HookResult.Continue;
        }

        private static HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
        {
            isTransmitRegistered = false;
            Instance.AddTimer(.1f, () => DisableAll());
            foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
            {
                var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (skillPlayer == null) continue;
                skillPlayer.IsDrawing = true;
            }

            Instance.RemoveListener<CheckTransmit>(CheckTransmit);
            int freezetime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<Int32>() ?? 0;
            Instance.AddTimer((Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0) + Math.Max(freezetime - Config.LoadedConfig.Settings.SkillTimeBeforeStart, 0) + .3f, SetSkill);
            return HookResult.Continue;
        }

        private static void DisableAll()
        {
            foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot))
            {
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo == null) return;
                Instance.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [player]);
                Instance.SkillAction(playerInfo.Skill.ToString(), "NewRound");
            }
        }

        private static HookResult RoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            if(SkillUtils.IsWarmup()) return HookResult.Continue;
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
                            if (skillInfo == null) continue;
                            skillsText += $" {ChatColors.DarkRed}{_player.PlayerName}{ChatColors.Lime}: {(_playerSkill.SpecialSkill == Skills.None || specialSkillInfo == null ? skillInfo.Name : $"{specialSkillInfo.Name} -> {skillInfo.Name}")}\n";
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
        }

        private static HookResult PlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerDeath", [@event]);

            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (victim == null || attacker == null) return HookResult.Continue;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
            if (playerInfo == null || playerInfo.IsDrawing) return HookResult.Continue;
            playerInfo.RandomPercentage = "";
            Instance.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [victim]);

            if (victim == attacker) return HookResult.Continue;
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
            return HookResult.Continue;
        }

        private static void CheckUseSkill(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            string? button = Config.LoadedConfig.Settings.AlternativeSkillButton;
            if (string.IsNullOrEmpty(button)) return;

            if (Enum.TryParse<PlayerButtons>(button, out var skillButton))
            {
                if (pressed != skillButton) return;
            } else return;

            if (player == null) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null || playerInfo.IsDrawing) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            Debug.WriteToDebug($"Player {player.PlayerName} used the skill: {playerInfo.Skill} by PlayerButtons: {pressed}");
            Instance.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
        }

        private static void EntitySpawned(CEntityInstance entity)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "OnEntitySpawned", [entity]);
        }

        private static void SetSkill()
        {
            if(SkillUtils.IsWarmup()) return;
            var validPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p .Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).ToList();

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

            foreach (var player in validPlayers)
            {
                if (player == null) continue;
                var teammates = validPlayers.Where(p => p.Team == player.Team && p != player);
                string teammateSkills = "";

                var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (skillPlayer == null) continue;
                skillPlayer.RandomPercentage = "";

                if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
                {
                    skillPlayer.Skill = Skills.None;
                    continue;
                }

                skillPlayer.IsDrawing = false;
                jSkill_SkillInfo randomSkill = new(Skills.None, Config.GetValue<string>(Skills.None, "color"), false);

                if (Instance?.GameRules != null && Instance?.GameRules.WarmupPeriod == false)
                {
                    Config.GameModes gameMode = (Config.GameModes)Config.LoadedConfig.Settings.GameMode;
                    if (staticSkills.TryGetValue(player.SteamID, out var staticSkill))
                        randomSkill = staticSkill;
                    else if (gameMode == Config.GameModes.Normal || gameMode == Config.GameModes.NoRepeat)
                    {
                        List<jSkill_SkillInfo> skillList = new(SkillData.Skills);
                        skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

                        if (validPlayers.Count(p => p.Team == player.Team) == 1)
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
                }

                //if (randomSkill.Display) SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{randomSkill.Name}{ChatColors.Lime}: {randomSkill.Description}", false);

                    skillPlayer.Skill = randomSkill.Skill;
                skillPlayer.SpecialSkill = Skills.None;
                Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", new [] { player });
                Debug.WriteToDebug($"Player {skillPlayer.PlayerName} has got the skill \"{randomSkill.Name}\".");
                skillPlayer.SkillDescriptionHudExpired = DateTime.Now.AddSeconds(Config.LoadedConfig.Settings.SkillDescriptionDuration);

                if (Config.LoadedConfig.Settings.TeamMateSkillInfo)
                {
                    Instance?.AddTimer(.3f, () =>
                    {
                        foreach (var teammate in teammates)
                        {
                            var teammateSkill = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == teammate.SteamID)?.Skill;
                            if (teammateSkill != null)
                            {
                                var skillInfo = SkillData.Skills.FirstOrDefault(p => p.Skill == teammateSkill);
                                teammateSkills += $" {ChatColors.Green}{teammate.PlayerName} - {ChatColors.DarkRed}{(skillInfo == null ? Skills.None : skillInfo.Name)} {ChatColors.White}| ";
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
            /*if (isTransmitRegistered)
                Instance?.RegisterListener<CheckTransmit>(CheckTransmit);*/
        }

        public static void SetRandomSkill(CCSPlayerController player)
        {
            var validPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).ToList();

            if (Config.LoadedConfig.Settings.GameMode == (int)Config.GameModes.TeamSkills)
            {
                List<jSkill_SkillInfo> tSkills = new(SkillData.Skills);
                tSkills.RemoveAll(s => s.Skill == tSkill.Skill || s.Skill == Skills.None || counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                tSkill = tSkills.Count == 0 ? noneSkill : tSkills[0];

                List<jSkill_SkillInfo> ctSkills = new(SkillData.Skills);
                ctSkills.RemoveAll(s => s.Skill == ctSkill.Skill || s.Skill == Skills.None || terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                ctSkill = ctSkills.Count == 0 ? noneSkill : ctSkills[0];
            }

            if (player == null) return;
            var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (skillPlayer == null) return;

            if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
            {
                skillPlayer.Skill = Skills.None;
                return;
            }

            jSkill_SkillInfo randomSkill = noneSkill;
            if (Instance?.GameRules != null && Instance?.GameRules.WarmupPeriod == false)
            {
                Config.GameModes gameMode = (Config.GameModes)Config.LoadedConfig.Settings.GameMode;
                if (staticSkills.TryGetValue(player.SteamID, out var staticSkill))
                    randomSkill = staticSkill;
                else if (gameMode == Config.GameModes.Normal || gameMode == Config.GameModes.NoRepeat)
                {
                    List<jSkill_SkillInfo> skillList = new(SkillData.Skills);
                    skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

                    if (validPlayers.Count(p => p.Team == player.Team) == 1)
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
                else if (gameMode == Config.GameModes.Debug)
                    return;
            }

            skillPlayer.Skill = randomSkill.Skill;
            skillPlayer.SpecialSkill = Skills.None;
            Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", new[] { player });
            Debug.WriteToDebug($"Player {skillPlayer.PlayerName} has got the skill \"{randomSkill.Name}\".");
            skillPlayer.SkillDescriptionHudExpired = DateTime.Now.AddSeconds(Config.LoadedConfig.Settings.SkillDescriptionDuration);
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var playerSkill in Instance.SkillPlayer)
                if (!playerSkill.IsDrawing)
                    Instance.SkillAction(playerSkill.Skill.ToString(), "CheckTransmit", [infoList]);
        }

        /*private static void ShowSkillDescription(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == playerInfo.Skill);
            if (skillData == null) return;

            var skillName = playerInfo.Skill.ToString().ToLower();
            var value = Math.Round((double)(playerInfo.SkillChance ?? 1), 2);
            var desc2 = Localization.GetTranslation($"{skillName}_desc2", value);
            var skilLDescription = desc2 == $"{skillName}_desc2"
                ? Localization.GetTranslation($"{skillName}_desc")
                : (desc2.Contains('%') ? desc2.Replace(value.ToString(), (value * 100).ToString()) : desc2);

            string infoLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("your_skill")}:</font> <br>";
            string skillLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = $"<font size='0.2' color='#999999'>{skilLDescription} <br>";

            var hudContent = infoLine + skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }*/
    }
}