using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static partial class Event
    {
        private static DateTime freezeTimeEnd = DateTime.MinValue;
        public static bool isTransmitRegistered = false;
        public static readonly jSkill_SkillInfo noneSkill = new(Skills.None, "Inwalida", "Nie posiadasz supermocy", "#FFFFFF");

        public static jSkill_SkillInfo[] terroristSkills => SkillData.Skills.Where(s => s.TeamNumber == 1).ToArray();
        public static jSkill_SkillInfo[] counterterroristSkills => SkillData.Skills.Where(s => s.TeamNumber == 2).ToArray();
        private static readonly object setLock = new();
        public static void Load()
        {
            Instance?.RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFull);
            Instance?.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnect);
            Instance?.RegisterEventHandler<EventRoundStart>(RoundStart);
            Instance?.RegisterEventHandler<EventRoundEnd>(RoundEnd);
            Instance?.RegisterEventHandler<EventPlayerDeath>(PlayerDeath);
            Instance?.RegisterEventHandler<EventPlayerBlind>(PlayerBlind);
            Instance?.RegisterEventHandler<EventPlayerHurt>(PlayerHurt);
            Instance?.RegisterEventHandler<EventPlayerJump>(PlayerJump);
            Instance?.RegisterEventHandler<EventWeaponFire>(WeaponFire);
            Instance?.RegisterEventHandler<EventItemEquip>(WeaponEquip);
            Instance?.RegisterEventHandler<EventItemPickup>(WeaponPickup);
            Instance?.RegisterEventHandler<EventWeaponReload>(WeaponReload);
            Instance?.RegisterEventHandler<EventGrenadeThrown>(GrenadeThrown);
            Instance?.RegisterEventHandler<EventBombBeginplant>(BombBeginplant);
            Instance?.RegisterEventHandler<EventBombPlanted>(BombPlanted);
            Instance?.RegisterEventHandler<EventBombBegindefuse>(BombBegindefuse);
            Instance?.RegisterEventHandler<EventDecoyStarted>(DecoyStarted);
            Instance?.RegisterEventHandler<EventDecoyDetonate>(DecoyDetonate);
            Instance?.RegisterEventHandler<EventSmokegrenadeDetonate>(SmokegrenadeDetonate);
            Instance?.RegisterEventHandler<EventSmokegrenadeExpired>(SmokegrenadeExpired);

            Instance?.RegisterListener<OnPlayerButtonsChanged>(CheckUseSkill);
            Instance?.RegisterListener<OnEntitySpawned>(EntitySpawned);
            Instance?.RegisterListener<OnTick>(OnTick);

            Instance?.HookUserMessage(208, PlayerMakeSound);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        }

        private static HookResult HandleSkillEvent(string eventName, object arg)
        {
            lock (setLock)
            {
                if (Instance?.SkillPlayer != null)
                {
                    foreach (var playerSkill in Instance.SkillPlayer)
                        if (!playerSkill.IsDrawing)
                            Instance.SkillAction(playerSkill.Skill.ToString(), eventName, [arg]);
                }
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerMakeSound(UserMessage um) => HandleSkillEvent("PlayerMakeSound", um);
        private static HookResult WeaponFire(EventWeaponFire @event, GameEventInfo info) => HandleSkillEvent("WeaponFire", @event);
        private static HookResult WeaponEquip(EventItemEquip @event, GameEventInfo info) => HandleSkillEvent("WeaponEquip", @event);
        private static HookResult WeaponPickup(EventItemPickup @event, GameEventInfo info) => HandleSkillEvent("WeaponPickup", @event);
        private static HookResult WeaponReload(EventWeaponReload @event, GameEventInfo info) => HandleSkillEvent("WeaponReload", @event);
        private static HookResult GrenadeThrown(EventGrenadeThrown @event, GameEventInfo info) => HandleSkillEvent("GrenadeThrown", @event);
        private static HookResult BombBeginplant(EventBombBeginplant @event, GameEventInfo info) => HandleSkillEvent("BombBeginplant", @event);
        private static HookResult BombPlanted(EventBombPlanted @event, GameEventInfo info) => HandleSkillEvent("BombPlanted", @event);
        private static HookResult BombBegindefuse(EventBombBegindefuse @event, GameEventInfo info) => HandleSkillEvent("BombBegindefuse", @event);
        private static HookResult DecoyStarted(EventDecoyStarted @event, GameEventInfo info) => HandleSkillEvent("DecoyStarted", @event);
        private static HookResult DecoyDetonate(EventDecoyDetonate @event, GameEventInfo info) => HandleSkillEvent("DecoyDetonate", @event);
        private static HookResult SmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info) => HandleSkillEvent("SmokegrenadeDetonate", @event);
        private static HookResult SmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info) => HandleSkillEvent("SmokegrenadeExpired", @event);
        private static HookResult PlayerHurt(EventPlayerHurt @event, GameEventInfo info) => HandleSkillEvent("PlayerHurt", @event);
        private static HookResult PlayerJump(EventPlayerJump @event, GameEventInfo info) => HandleSkillEvent("PlayerJump", @event);
        private static HookResult PlayerBlind(EventPlayerBlind @event, GameEventInfo info) => HandleSkillEvent("PlayerBlind", @event);
        private static HookResult OnTakeDamage(DynamicHook h) => HandleSkillEvent("OnTakeDamage", h);

        private static void OnTick()
        {
            lock (setLock)
            {
                if (Instance?.SkillPlayer != null)
                {
                    foreach (var playerSkill in Instance.SkillPlayer)
                        if (!playerSkill.IsDrawing)
                            Instance.SkillAction(playerSkill.Skill.ToString(), "OnTick");
                }
            }
        }

        private static HookResult PlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                Instance?.SkillPlayer.Add(new jSkill_PlayerInfo
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
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (skillPlayer == null) return HookResult.Continue;

                var items = Instance?.SkillPlayer.ToList();
                if (Instance != null && items != null)
                {
                    Instance.SkillPlayer = [.. items.Where(p => p.SteamID != skillPlayer.SteamID)];
                }

                return HookResult.Continue;
            }
        }

        private static HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
        {
            lock (setLock)
            {
                isTransmitRegistered = false;
                Instance?.AddTimer(.1f, DisableAll);
                foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
                {
                    var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (skillPlayer == null) continue;
                    skillPlayer.IsDrawing = true;
                }

                Instance?.RemoveListener<CheckTransmit>(CheckTransmit);
                int freezetime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 0;
                freezeTimeEnd = DateTime.Now.AddSeconds(freezetime + (Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0));
                Instance?.AddTimer((Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0) + Math.Max(freezetime - 4, 0) + .3f, SetSkill);
                return HookResult.Continue;
            }
        }

        private static void DisableAll()
        {
            lock (setLock)
            {
                foreach (var player in Utilities.GetPlayers().Where(p => !p.IsBot))
                {
                    var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (playerInfo == null) return;
                    Instance?.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [player]);
                    Instance?.SkillAction(playerInfo.Skill.ToString(), "NewRound");
                }
            }
        }

        private static HookResult RoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            lock (setLock)
            {
                if (SkillUtils.IsWarmup()) return HookResult.Continue;
                foreach (var player in Utilities.GetPlayers())
                {
                    Instance?.AddTimer(0.5f, () =>
                    {
                        var _players = Utilities.GetPlayers().Where(p => p.IsValid).OrderBy(p => p.Team);

                        string skillsText = "";
                        foreach (var _player in _players)
                        {
                            var _playerSkill = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == _player.SteamID);
                            if (_playerSkill != null)
                            {
                                var skillInfo = SkillData.Skills.FirstOrDefault(p => p.Skill == _playerSkill.Skill);
                                var specialSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == _playerSkill.SpecialSkill);
                                if (skillInfo == null) continue;
                                skillsText += $" {ChatColors.DarkRed}{_player.PlayerName}{ChatColors.Lime}: {(_playerSkill.SpecialSkill == Skills.None || specialSkillInfo == null ? skillInfo.Name : $"{specialSkillInfo.Name} -> {skillInfo.Name}")}\n";
                            }
                        }
                    });
                }
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            lock (setLock)
            {
                if (Instance?.SkillPlayer != null)
                {
                    foreach (var playerSkill in Instance.SkillPlayer)
                        if (!playerSkill.IsDrawing)
                            Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerDeath", [@event]);

                }

                var victim = @event.Userid;
                var attacker = @event.Attacker;
                if (victim == null || attacker == null) return HookResult.Continue;

                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
                if (playerInfo == null || playerInfo.IsDrawing) return HookResult.Continue;
                playerInfo.RandomPercentage = "";
                Instance?.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [victim]);

                if (victim == attacker) return HookResult.Continue;
                if (!SkillUtils.IsWarmup())
                {
                    var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
                    if (attackerInfo != null)
                    {
                        var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.Skill);
                        var specialSkillData = SkillData.Skills.FirstOrDefault(s => s.Skill == attackerInfo.SpecialSkill);
                        if (skillData == null || specialSkillData == null) return HookResult.Continue;
                        string skillDesc = skillData.Description;

                        SkillUtils.PrintToChat(victim, $"{ChatColors.DarkRed}{attacker.PlayerName}{ChatColors.Lime} posiada:", false);
                        SkillUtils.PrintToChat(victim, $"{ChatColors.DarkRed}{(attackerInfo.SpecialSkill == Skills.None ? skillData.Name : $"{specialSkillData.Name} -> {skillData.Name}")}{ChatColors.Lime} - {skillDesc}", false);
                    }
                }
                return HookResult.Continue;
            }
        }

        private static void CheckUseSkill(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            lock (setLock)
            {
                string? button = "Inspect";
                if (string.IsNullOrEmpty(button)) return;

                if (Enum.TryParse<PlayerButtons>(button, out var skillButton))
                {
                    if (pressed != skillButton) return;
                }
                else return;

                if (player == null) return;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo == null || playerInfo.IsDrawing) return;

                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn?.CBodyComponent == null) return;
                if (!player.IsValid || !player.PawnIsAlive) return;
                if (SkillUtils.IsFreezetime()) return;

                Debug.WriteToDebug($"Player {player.PlayerName} used the skill: {playerInfo.Skill} by PlayerButtons: {pressed}");
                Instance?.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
            }
        }

        private static void EntitySpawned(CEntityInstance entity)
        {
            lock (setLock)
            {
                if (Instance?.SkillPlayer != null)
                {
                    foreach (var playerSkill in Instance.SkillPlayer)
                        if (!playerSkill.IsDrawing)
                            Instance.SkillAction(playerSkill.Skill.ToString(), "OnEntitySpawned", [entity]);
                }
            }
        }

        private static jSkill_SkillInfo GetRandomSkill(CCSPlayerController player, jSkill_PlayerInfo skillPlayer)
        {
            if (Instance?.GameRules != null && !SkillUtils.IsWarmup())
            {
                List<jSkill_SkillInfo> skillList = [.. SkillData.Skills];
                skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

                if (player.Team == CsTeam.Terrorist)
                    skillList.RemoveAll(s => counterterroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));
                else
                    skillList.RemoveAll(s => terroristSkills.Any(s2 => s2.Name == s.Skill.ToString()));

                return skillList.Count == 0 ? noneSkill : skillList[Instance.Random.Next(skillList.Count)];
            }
            return noneSkill;
        }

        private static void SetSkill()
        {
            lock (setLock)
            {
                if (SkillUtils.IsWarmup()) return;
                var validPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).ToList();

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
                    var randomSkill = GetRandomSkill(player, skillPlayer);

                    skillPlayer.Skill = randomSkill.Skill;
                    player.EmitSound("UIPanorama.tab_mainmenu_news", volume: 0.3f);
                    skillPlayer.SpecialSkill = Skills.None;
                    Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);
                    Debug.WriteToDebug($"{skillPlayer.PlayerName} posiada \"{randomSkill.Name}\".");

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
                            SkillUtils.PrintToChat(player, $" {ChatColors.Lime}Supermoce kolegów:", false);
                            foreach (string text in teammateSkills.Split("\n"))
                                if (!string.IsNullOrEmpty(text))
                                    player.PrintToChat(text);
                        }
                    });
                }
            }
        }

        public static void SetRandomSkill(CCSPlayerController player)
        {
            lock (setLock)
            {
                if (player == null) return;
                var skillPlayer = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (skillPlayer == null) return;
                skillPlayer.RandomPercentage = "";

                if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
                {
                    skillPlayer.Skill = Skills.None;
                    return;
                }

                var randomSkill = GetRandomSkill(player, skillPlayer);

                skillPlayer.Skill = randomSkill.Skill;
                skillPlayer.SpecialSkill = Skills.None;
                Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);
                Debug.WriteToDebug($"{skillPlayer.PlayerName} posiada \"{randomSkill.Name}\".");
            }
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            lock (setLock)
            {
                if (Instance?.SkillPlayer != null)
                {
                    foreach (var playerSkill in Instance.SkillPlayer)
                        if (!playerSkill.IsDrawing)
                            Instance.SkillAction(playerSkill.Skill.ToString(), "CheckTransmit", [infoList]);
                }
            }
        }

        public static void EnableTransmit()
        {
            if (!isTransmitRegistered)
            {
                Instance?.RegisterListener<CheckTransmit>(CheckTransmit);
                isTransmitRegistered = true;
            }
        }
        
        public static DateTime GetFreezeTimeEnd() => freezeTimeEnd;
    }
}