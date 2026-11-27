using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static CounterStrikeSharp.API.Core.Listeners;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public static partial class Event
    {
        public static bool isTransmitRegistered = false;
        public static readonly SkillInfo noneSkill = new(Skills.None, "Inwalida", "Nie posiadasz supermocy", "#FFFFFF");
        private static readonly object setLock = new();
        private static readonly HashSet<int> _botSkillAttempted = [];
        private static readonly ConcurrentDictionary<int, float> _botNextActive = [];
        public static void Load()
        {
            Instance?.RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFull);
            Instance?.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnect);
            Instance?.RegisterEventHandler<EventBotTakeover>(BotTakeover);
            Instance?.RegisterEventHandler<EventPlayerSpawn>(PlayerSpawn);
            Instance?.RegisterEventHandler<EventRoundEnd>(RoundEnd);
            Instance?.RegisterEventHandler<EventRoundStart>(RoundStart);
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
            Instance?.RegisterEventHandler<EventBombAbortplant>(BombAbortplant);
            Instance?.RegisterEventHandler<EventBombAbortdefuse>(BombAbortdefuse);
            Instance?.RegisterEventHandler<EventBombDefused>(BombDefused);
            Instance?.RegisterEventHandler<EventBombExploded>(BombExploded);
            Instance?.RegisterEventHandler<EventBombDropped>(BombDropped);
            Instance?.RegisterEventHandler<EventBombPickup>(BombPickup);
            Instance?.RegisterEventHandler<EventHostageFollows>(HostageFollows);
            Instance?.RegisterEventHandler<EventHostageRescued>(HostageRescued);
            Instance?.RegisterEventHandler<EventHostageRescuedAll>(HostageRescuedAll);
            Instance?.RegisterEventHandler<EventHostageKilled>(HostageKilled);
            Instance?.RegisterEventHandler<EventHostageHurt>(HostageHurt);
            Instance?.RegisterEventHandler<EventHostageStopsFollowing>(HostageStopsFollowing);
            Instance?.RegisterEventHandler<EventEnterBombzone>(EnterBombzone);
            Instance?.RegisterEventHandler<EventExitBombzone>(ExitBombzone);
            Instance?.RegisterEventHandler<EventDecoyStarted>(DecoyStarted);
            Instance?.RegisterEventHandler<EventDecoyDetonate>(DecoyDetonate);
            Instance?.RegisterEventHandler<EventMolotovDetonate>(MolotovDetonate);
            Instance?.RegisterEventHandler<EventSmokegrenadeDetonate>(SmokegrenadeDetonate);
            Instance?.RegisterEventHandler<EventSmokegrenadeExpired>(SmokegrenadeExpired);

            Instance?.RegisterListener<OnPlayerButtonsChanged>(CheckUseSkill);
            Instance?.RegisterListener<OnEntitySpawned>(EntitySpawned);
            Instance?.RegisterListener<OnTick>(OnTick);

            Instance?.HookUserMessage(208, PlayerMakeSound);
            Instance?.RegisterListener<OnPlayerTakeDamagePre>(OnPlayerTakeDamage);
            Instance?.RegisterListener<OnEntityTakeDamagePre>(OnEntityTakeDamage);
        }

        private static HookResult HandleSkillEvent(SkillEvent eventName, params object[] args)
        {
            var snapshot = SkillUtils.GetSkillSnapshot();
            if (snapshot.Length == 0)
                return HookResult.Continue;

            foreach (var playerSkill in snapshot)
                if (!playerSkill.IsDrawing && SkillRegistry.HasEvent(eventName, playerSkill.Skill))
                    SkillRegistry.Dispatch(playerSkill.Skill, eventName, args);

            return HookResult.Continue;
        }

        private static HookResult HandleBombStateEvent(SkillEvent eventName, object @event)
        {
            GameState.Refresh();
            if (eventName is SkillEvent.BombDefused or SkillEvent.BombExploded)
                Bomb.Reset();
            else
                Bomb.Refresh();

            return HandleSkillEvent(eventName, @event);
        }

        private static HookResult PlayerMakeSound(UserMessage um) => HandleSkillEvent(SkillEvent.PlayerMakeSound, um);
        private static HookResult WeaponFire(EventWeaponFire @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.WeaponFire, @event);
        private static HookResult WeaponEquip(EventItemEquip @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.WeaponEquip, @event);
        private static HookResult WeaponPickup(EventItemPickup @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.WeaponPickup, @event);
        private static HookResult WeaponReload(EventWeaponReload @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.WeaponReload, @event);
        private static HookResult GrenadeThrown(EventGrenadeThrown @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.GrenadeThrown, @event);
        private static HookResult BombBeginplant(EventBombBeginplant @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.BombBeginplant, @event);
        private static HookResult BombPlanted(EventBombPlanted @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombPlanted, @event);
        private static HookResult BombBegindefuse(EventBombBegindefuse @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombBegindefuse, @event);
        private static HookResult BombAbortplant(EventBombAbortplant @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombAbortplant, @event);
        private static HookResult BombAbortdefuse(EventBombAbortdefuse @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombAbortdefuse, @event);
        private static HookResult BombDefused(EventBombDefused @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombDefused, @event);
        private static HookResult BombExploded(EventBombExploded @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.BombExploded, @event);
        private static HookResult BombDropped(EventBombDropped @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.BombDropped, @event);
        private static HookResult BombPickup(EventBombPickup @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.BombPickup, @event);
        private static HookResult HostageFollows(EventHostageFollows @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.HostageFollows, @event);
        private static HookResult HostageRescued(EventHostageRescued @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.HostageRescued, @event);
        private static HookResult HostageRescuedAll(EventHostageRescuedAll @event, GameEventInfo info) => HandleBombStateEvent(SkillEvent.HostageRescuedAll, @event);
        private static HookResult HostageKilled(EventHostageKilled @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.HostageKilled, @event);
        private static HookResult HostageHurt(EventHostageHurt @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.HostageHurt, @event);
        private static HookResult HostageStopsFollowing(EventHostageStopsFollowing @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.HostageStopsFollowing, @event);
        private static HookResult EnterBombzone(EventEnterBombzone @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.EnterBombzone, @event);
        private static HookResult ExitBombzone(EventExitBombzone @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.ExitBombzone, @event);
        private static HookResult DecoyStarted(EventDecoyStarted @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.DecoyStarted, @event);
        private static HookResult DecoyDetonate(EventDecoyDetonate @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.DecoyDetonate, @event);
        private static HookResult MolotovDetonate(EventMolotovDetonate @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.MolotovDetonate, @event);
        private static HookResult SmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.SmokegrenadeDetonate, @event);
        private static HookResult SmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.SmokegrenadeExpired, @event);
        private static HookResult PlayerHurt(EventPlayerHurt @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.PlayerHurt, @event);
        private static HookResult PlayerJump(EventPlayerJump @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.PlayerJump, @event);
        private static HookResult PlayerBlind(EventPlayerBlind @event, GameEventInfo info) => HandleSkillEvent(SkillEvent.PlayerBlind, @event);
        private static HookResult OnPlayerTakeDamage(CCSPlayerPawn victim, CTakeDamageInfo info) => HandleSkillEvent(SkillEvent.OnPlayerTakeDamage, victim, info);
        private static HookResult OnEntityTakeDamage(CEntityInstance entity, CTakeDamageInfo info) => HandleSkillEvent(SkillEvent.OnEntityTakeDamage, entity, info);

        private static void OnTick()
        {
            ReconcileBots();
            UseBotActiveSkills();
            var snapshot = SkillUtils.GetSkillSnapshot();
            MenuSkillFramework.OnTick();

            if (snapshot.Length == 0) return;

            foreach (var playerSkill in snapshot)
                if (!playerSkill.IsDrawing && SkillRegistry.HasOnTick(playerSkill.Skill))
                    SkillRegistry.Dispatch(playerSkill.Skill, SkillEvent.OnTick);
        }

        public static SkillPlayerInfo? EnsurePlayerInfo(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || Instance == null) return null;

            if (Instance.SkillPlayer.TryGetValue(player.Slot, out var existing))
            {
                existing.PlayerName = player.PlayerName;
                existing.IsBot = player.IsBot;
                return existing;
            }

            var info = new SkillPlayerInfo
            {
                Slot = player.Slot,
                PlayerName = player.PlayerName,
                IsBot = player.IsBot,
                Skill = Skills.None,
                SpecialSkill = Skills.None,
                IsDrawing = false,
                SkillChance = 1,
                RandomPercentage = "",
            };
            Instance.SkillPlayer.TryAdd(player.Slot, info);
            return info;
        }

        private static HookResult PlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                EnsurePlayerInfo(player);

                if (!player.IsBot)
                {
                    SkillUtils.PrintToChat(player, $"UWAGA! By zbindować supermoce na własny klawisz.");
                    SkillUtils.PrintToChat(player, $"wyjdź z serwera i wpisz w konsolę {ChatColors.Yellow}bind v css_useskill{ChatColors.Lime} w menu głównym gdzie V to klawisz.");
                }
                return HookResult.Continue;
            }
        }

        private static HookResult BotTakeover(EventBotTakeover @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var human = @event.Userid;
                var bot = @event.Botid;

                if (bot != null && bot.IsValid)
                {
                    var botInfo = EnsurePlayerInfo(bot);
                    if (botInfo != null && botInfo.Skill != Skills.None)
                    {
                        var humanInfo = EnsurePlayerInfo(human);
                        if (human != null && humanInfo != null && humanInfo.Skill == Skills.None)
                        {
                            humanInfo.Skill = botInfo.Skill;
                            humanInfo.SpecialSkill = botInfo.SpecialSkill;
                            humanInfo.SkillChance = 0;
                            humanInfo.RandomPercentage = botInfo.RandomPercentage;
                            Instance?.SkillAction(botInfo.Skill.ToString(), "EnableSkill", [human]);
                        }

                        Instance?.SkillAction(botInfo.Skill.ToString(), "DisableSkill", [bot]);
                    }

                    if (botInfo != null)
                    {
                        botInfo.Skill = Skills.None;
                        botInfo.SpecialSkill = Skills.None;
                        botInfo.IsDrawing = false;
                        botInfo.RandomPercentage = "";
                    }
                }
                else if (human != null)
                {
                    EnsurePlayerInfo(human);
                }

                SkillUtils.ForceRefresh();
                return HookResult.Continue;
            }
        }

        private static HookResult PlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            lock (setLock)
            {
                var player = @event.Userid;
                if (player == null || !player.IsValid) return HookResult.Continue;

                var skillPlayer = SkillUtils.GetPlayerInfo(player);
                if (skillPlayer == null) return HookResult.Continue;

                Instance?.SkillPlayer.TryRemove(skillPlayer.Slot, out _);

                return HookResult.Continue;
            }
        }

        private static HookResult PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return HookResult.Continue;

            // A round start gives a fresh pawn; the spectator row and its toast belong to the old life.
            SpectatorSkillPanel.ResetForSpawn(player);

            lock (setLock)
            {
                var playerInfo = EnsurePlayerInfo(player);
                if (playerInfo == null || playerInfo.IsDrawing) return HookResult.Continue;
                if (SkillUtils.IsWarmup() || GameState.FreezePeriod || GameState.TeamIntroPeriod)
                    return HookResult.Continue;

                if (playerInfo.Skill == Skills.None)
                    SetRandomSkill(player);

                return HookResult.Continue;
            }
        }

        private static HookResult RoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            SpectatorSkillPanel.OnRoundEnd();
            return HookResult.Continue;
        }

        private static HookResult RoundStart(EventRoundStart @event, GameEventInfo info)
        {
            SpectatorSkillPanel.OnRoundStart();
            lock (setLock)
            {
                isTransmitRegistered = false;
                _botSkillAttempted.Clear();
                _botNextActive.Clear();
                Bomb.Reset();
                Instance?.AddTimer(.1f, DisableAll);
                foreach (var player in SkillUtils.CachedPlayers.Where(p => p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
                {
                    var skillPlayer = EnsurePlayerInfo(player);
                    if (skillPlayer == null) continue;
                    skillPlayer.IsDrawing = true;
                }

                Instance?.RemoveListener<CheckTransmit>(CheckTransmit);
                GameState.Refresh();
                int freezetime = ConVar<int>.Find("mp_freezetime")?.Value ?? 0;
                Instance?.AddTimer((Instance?.GameRules?.TeamIntroPeriod == true ? 7 : 0) + Math.Max(freezetime - 6, 0) + .3f, SetSkill);
                return HookResult.Continue;
            }
        }

        private static void DisableAll()
        {
            lock (setLock)
            {
                foreach (var player in SkillUtils.CachedPlayers)
                {
                    var playerInfo = EnsurePlayerInfo(player);
                    if (playerInfo == null) continue;
                    Instance?.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [player]);
                    Instance?.SkillAction(playerInfo.Skill.ToString(), "NewRound");
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null) continue;
                    pawn.VelocityModifier = 1f;
                    pawn.ActualGravityScale = 1f;
                    pawn.Health = 100;
                    pawn.MaxHealth = 100;
                    SkillUtils.ChangePlayerScale(player, 1f);

                }

                PassiveSkillFramework.OnNewRound();
                ActiveSkillFramework.OnNewRound();
                MenuSkillFramework.OnNewRound();
            }
        }

        private static HookResult PlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (Instance?.SkillPlayer != null)
            {
                var snapshot = SkillUtils.GetSkillSnapshot();
                foreach (var playerSkill in snapshot)
                    if (!playerSkill.IsDrawing)
                        Instance.SkillAction(playerSkill.Skill.ToString(), "PlayerDeath", [@event]);
            }

            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (victim == null || attacker == null) return HookResult.Continue;

            var playerInfo = SkillUtils.GetPlayerInfo(victim);
            if (playerInfo == null || playerInfo.IsDrawing) return HookResult.Continue;
            playerInfo.RandomPercentage = "";
            Instance?.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [victim]);

            if (victim == attacker) return HookResult.Continue;
            if (!SkillUtils.IsWarmup())
            {
                var attackerInfo = SkillUtils.GetPlayerInfo(attacker);
                if (attackerInfo != null)
                {
                    var skillData = SkillData.Find(attackerInfo.Skill);
                    var specialSkillData = SkillData.Find(attackerInfo.SpecialSkill);
                    if (skillData == null || specialSkillData == null) return HookResult.Continue;
                    string skillDesc = skillData.Description;

                    SkillUtils.PrintToChat(victim, $"{ChatColors.DarkRed}{attacker.PlayerName}{ChatColors.Lime} ma {ChatColors.DarkRed}{(attackerInfo.SpecialSkill == Skills.None ? skillData.Name : $"{specialSkillData.Name} -> {skillData.Name}")}", false);
                    SkillUtils.PrintToChat(victim, $"{skillDesc}", false);
                }
            }
            return HookResult.Continue;
        }

        private static void CheckUseSkill(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            lock (setLock)
            {
                if (!pressed.HasFlag(PlayerButtons.Inspect)) return;
                if (SkillUtils.IsFreezetime() == true) return;

                if (player == null) return;
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo == null || playerInfo.IsDrawing) return;

                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn?.CBodyComponent == null) return;
                if (!player.IsValid || !player.PawnIsAlive) return;

                Instance?.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);
            }
        }

        private static void EntitySpawned(CEntityInstance entity)
        {
            var snapshot = SkillUtils.GetSkillSnapshot();
            if (snapshot.Length == 0) return;

            foreach (var playerSkill in snapshot)
                if (!playerSkill.IsDrawing && SkillRegistry.HasEvent(SkillEvent.OnEntitySpawned, playerSkill.Skill))
                    SkillRegistry.Dispatch(playerSkill.Skill, SkillEvent.OnEntitySpawned, [entity]);
        }

        /// <summary>
        /// Active skills normally need an Inspect press; bots have no keyboard, so
        /// let them cast on a coarse random cadence once the cooldown is ready.
        /// </summary>
        private static void UseBotActiveSkills()
        {
            if (SkillUtils.IsWarmup() || GameState.FreezePeriod || GameState.TeamIntroPeriod)
                return;

            float now = Server.CurrentTime;
            foreach (var bot in SkillUtils.CachedPlayers)
            {
                if (!bot.IsBot || !bot.IsValid || !bot.PawnIsAlive) continue;
                if (bot.Team is not (CsTeam.CounterTerrorist or CsTeam.Terrorist)) continue;

                var info = SkillUtils.GetPlayerInfo(bot);
                if (info == null || info.IsDrawing || info.Skill == Skills.None) continue;
                if (!SkillData.ActiveSkillConfigs.ContainsKey(info.Skill)) continue;
                if (!ActiveSkillFramework.CanUseSkill(info.Skill, bot)) continue;

                if (_botNextActive.TryGetValue(bot.Slot, out float next) && next > now) continue;
                _botNextActive[bot.Slot] = now + 2f;

                if ((Supermoce.Instance?.Random.NextDouble() ?? 1.0) > 0.2) continue;
                Supermoce.Instance?.SkillAction(info.Skill.ToString(), "UseSkill", [bot]);
            }
        }

        private static void ReconcileBots()
        {
            if (SkillUtils.IsWarmup() || GameState.FreezePeriod || GameState.TeamIntroPeriod)
                return;

            foreach (var bot in SkillUtils.CachedPlayers)
            {
                if (!bot.IsBot || !bot.IsValid || !bot.PawnIsAlive) continue;
                if (bot.Team is not (CsTeam.CounterTerrorist or CsTeam.Terrorist)) continue;
                if (!_botSkillAttempted.Add(bot.Slot)) continue;

                var info = EnsurePlayerInfo(bot);
                if (info == null || info.IsDrawing || info.Skill != Skills.None) continue;

                SetRandomSkill(bot);
            }
        }

        private static SkillInfo GetRandomSkill(CCSPlayerController player, SkillPlayerInfo skillPlayer)
        {
            if (Instance?.GameRules != null && !SkillUtils.IsWarmup())
            {
                List<SkillInfo> skillList = [.. SkillData.Skills];
                skillList.RemoveAll(s => s?.Skill == skillPlayer?.Skill || s?.Skill == skillPlayer?.SpecialSkill || s?.Skill == Skills.None);

                skillList.RemoveAll(s => 
                    (player.Team == CsTeam.Terrorist && s.TeamNumber == 2) ||  
                    (player.Team == CsTeam.CounterTerrorist && s.TeamNumber == 1)
                );

                bool isBombMap = GameState.IsBombMap;
                bool isHostageMap = GameState.IsHostageMap;
                skillList.RemoveAll(s =>
                    (s.Objective == 1 && !isBombMap) ||
                    (s.Objective == 2 && !isHostageMap)
                );

                return skillList.Count == 0 ? noneSkill : skillList[Instance.Random.Next(skillList.Count)];
            }
            return noneSkill;
        }

        private static void SetSkill()
        {
            lock (setLock)
            {
                if (SkillUtils.IsWarmup()) return;
                var validPlayers = SkillUtils.CachedPlayers.Where(p => p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist).ToList();

                foreach (var player in validPlayers)
                {
                    if (player == null) continue;

                    var skillPlayer = EnsurePlayerInfo(player);
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
                    skillPlayer.SkillChance = 0;
                    if (!player.IsBot)
                        player.EmitSound("UIPanorama.tab_mainmenu_news", volume: 0.1f);
                    skillPlayer.SpecialSkill = Skills.None;
                    Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);
                }

                TeamPowersPanel.ShowForTeams();
            }
        }

        public static void SetRandomSkill(CCSPlayerController player)
        {
            lock (setLock)
            {
                if (player == null) return;
                var skillPlayer = EnsurePlayerInfo(player);
                if (skillPlayer == null) return;
                skillPlayer.RandomPercentage = "";

                if (player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid)
                {
                    skillPlayer.Skill = Skills.None;
                    return;
                }

                var randomSkill = GetRandomSkill(player, skillPlayer);

                skillPlayer.Skill = randomSkill.Skill;
                skillPlayer.SkillChance = 0;
                skillPlayer.SpecialSkill = Skills.None;
                Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);
            }
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            var snapshot = SkillUtils.GetSkillSnapshot();
            if (snapshot.Length == 0) return;

            foreach (var playerSkill in snapshot)
                if (!playerSkill.IsDrawing && SkillRegistry.HasEvent(SkillEvent.CheckTransmit, playerSkill.Skill))
                    SkillRegistry.Dispatch(playerSkill.Skill, SkillEvent.CheckTransmit, [infoList]);
        }

        public static void EnableTransmit()
        {
            if (!isTransmitRegistered)
            {
                Instance?.RegisterListener<CheckTransmit>(CheckTransmit);
                isTransmitRegistered = true;
            }
        }
    }
}
