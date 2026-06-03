using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using WASDMenuAPI.Classes;
using WASDSharedAPI;

namespace Supermoce
{
    public static class SkillUtils
    {
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> HEGrenadeProjectile_CreateFunc = new(GameData.GetSignature("HEGrenadeProjectile_CreateFunc"));
        private static readonly MemoryFunctionVoid<nint, float, RoundEndReason, nint, nint> TerminateRoundFunc = new(GameData.GetSignature("CCSGameRules_TerminateRound"));
        public static CCSPlayerController[] CachedPlayers { get; private set; } = [];
        public static int CurrentTick { get; private set; }
        private static SkillPlayerInfo[] _cachedSkillSnapshot = [];
        private static readonly ConcurrentDictionary<ulong, SkillPlayerInfo> _playerInfoCache = [];
        public static void RefreshTickState()
        {
            CurrentTick = Server.TickCount;
            CachedPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV).ToArray();
            _cachedSkillSnapshot = Supermoce.Instance?.SkillPlayer?.Values.ToArray() ?? [];
            _playerInfoCache.Clear();
            foreach (var sp in _cachedSkillSnapshot)
                _playerInfoCache.TryAdd(sp.SteamID, sp);
        }
        public static SkillPlayerInfo? GetPlayerInfo(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid) return null;
            _playerInfoCache.TryGetValue(player.SteamID, out var info);
            return info;
        }
        public static SkillPlayerInfo[] GetSkillSnapshot() => _cachedSkillSnapshot;
        private static SkillInfo[] _cachedSkillsArray = [];
        private static int _cachedSkillsHash;
        public static SkillInfo[] GetCachedSkillsArray()
        {
            int currentCount = SkillData.Skills.Count;
            if (_cachedSkillsHash != currentCount)
            {
                _cachedSkillsArray = [.. SkillData.Skills];
                _cachedSkillsHash = currentCount;
            }
            return _cachedSkillsArray;
        }
        private static string ChatIcon(bool isError) => isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}►{ChatColors.Lime}";

        public static void PrintToChat(CCSPlayerController player, string msg, bool isError = false)
        {
            player.PrintToChat($"{ChatIcon(isError)} {msg}");
        }
        public static void PrintToChatAll(string msg, bool isError = false)
        {
            Server.PrintToChatAll($"{ChatIcon(isError)} {msg}");
        }
        public static void RegisterSkill(Skills skill, string name, string desc, string color, byte teamnum = 0, byte objective = 0)
        {
            if (!SkillData.Skills.Any(s => s.Skill == skill))
            {
                SkillData.Skills.Add(new SkillInfo(skill, name, desc, color, teamnum, objective));
                _cachedSkillsHash = 0;
            }
        }
        public static void RegisterPassiveSkill(
            Skills skill,
            string name,
            string description,
            string color,
            byte teamnum = 0,
            byte objective = 0,
            int minValue = 0,
            int maxValue = 0,
            int step = 1,
            Func<int, string>? customValueFormatter = null
            )
        {
            RegisterSkill(skill, name, description, color, teamnum, objective);
            var config = new PassiveSkillConfig
            {
                MinValue = minValue,
                MaxValue = maxValue,
                Step = step,
                CustomValueFormatter = customValueFormatter
            };
            SkillData.PassiveSkillConfigs[skill] = config;
        }
        public static void RegisterActiveSkill(
            Skills skill,
            string name,
            string description,
            string color,
            byte teamnum = 0,
            byte objective = 0,
            int minCooldown = 15,
            int maxCooldown = 50,
            int cooldownStep = 5,
            bool useCustomHud = false)
        {
            RegisterSkill(skill, name, description, color, teamnum, objective);
            var config = new ActiveSkillConfig
            {
                MinCooldown = minCooldown,
                MaxCooldown = maxCooldown,
                CooldownStep = cooldownStep,
                UseCustomHud = useCustomHud,
            };
            SkillData.ActiveSkillConfigs[skill] = config;
        }

        public static void RegisterMenuSkill(
            Skills skill,
            string name,
            string description,
            string color,
            byte teamnum = 0,
            byte objective = 0)
        {
            RegisterSkill(skill, name, description, color, teamnum, objective);
        }
        public static PassiveSkillConfig? GetPassiveSkillConfig(Skills skill)
        {
            SkillData.PassiveSkillConfigs.TryGetValue(skill, out var config);
            return config;
        }
        public static ActiveSkillConfig? GetActiveSkillConfig(Skills skill)
        {
            SkillData.ActiveSkillConfigs.TryGetValue(skill, out var config);
            return config;
        }
        public static void TryGiveWeapon(CCSPlayerController player, CsItem item, int count = 1)
        {
            string? itemString = EnumUtils.GetEnumMemberAttributeValue(item);
            if (string.IsNullOrWhiteSpace(itemString)) return;

            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            if (player.PlayerPawn.Value.WeaponServices == null) return;

            var exists = player.PlayerPawn.Value.WeaponServices.MyWeapons
                .FirstOrDefault(w => w != null && w.IsValid && w.Value != null && w.Value.IsValid && w.Value.DesignerName == itemString);
            if (exists == null)
                for (int i = 0; i < count; i++)
                    player.GiveNamedItem(item);
        }
        public static double GetDistance(Vector vector1, Vector vector2) =>
            Math.Sqrt(Math.Pow(vector2.X - vector1.X, 2) + Math.Pow(vector2.Y - vector1.Y, 2) + Math.Pow(vector2.Z - vector1.Z, 2));
        public static string SecondsToTimer(int totalSeconds) =>
            totalSeconds <= 0 ? "00:00" : $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
        public static void SafeKillEntity<T>(uint? index) where T : CBaseEntity
        {
            if (index == null) return;

            var ent = Utilities.GetEntityFromIndex<T>((int)index);
            if (ent == null || !ent.IsValid) return;

            ent.AddEntityIOEvent("Kill", ent, delay: 0.1f);
        }
        public static Vector GetForwardVector(QAngle angles)
        {
            float pitch = -angles.X * (float)(Math.PI / 180);
            float yaw = angles.Y * (float)(Math.PI / 180);

            float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = (float)(Math.Cos(pitch) * Math.Sin(yaw));
            float z = (float)Math.Sin(pitch);

            return new Vector(x, y, z);
        }
        public static void ChangePlayerScale(CCSPlayerController? player, float scale)
        {
            if (player == null || !player.IsValid) return;
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.CBodyComponent == null || playerPawn.CBodyComponent.SceneNode == null) return;

            float appliedScale = Math.Max(scale, 0.01f);

            playerPawn.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = appliedScale;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");

            Server.NextFrame(() => playerPawn.AcceptInput("SetScale", playerPawn, playerPawn, appliedScale.ToString(CultureInfo.InvariantCulture)));
        }
        public static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
        {
            HEGrenadeProjectile_CreateFunc.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, 44, teamNum);
        }
        public static void TakeHealth(CCSPlayerPawn? pawn, int damage, bool playSound = true)
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            int newHealth = pawn.Health - damage;
            pawn.Health = newHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            if (playSound)
                pawn.EmitSound("Player.DamageBody.Onlooker", volume: 0.1f);

            if (pawn.Health <= 0)
                Server.NextFrame(() =>
                {
                    pawn?.CommitSuicide(false, true); // NOTE: no kill credit is awarded to the damage source
                });
        }
        public static void AddHealth(CCSPlayerPawn? pawn, int extraHealth, int maxHealth = 100)
        {
            if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            int currentMax = pawn.MaxHealth;
            if (maxHealth > currentMax)
            {
                pawn.MaxHealth = maxHealth;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            }

            pawn.Health = Math.Min(pawn.Health + extraHealth, pawn.MaxHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        public static string GetDesignerName(CBasePlayerWeapon? weapon)
        {
            if (weapon?.IsValid != true) return string.Empty;
            string designerName = weapon.DesignerName;
            ushort index = weapon.AttributeManager.Item.ItemDefinitionIndex;

            designerName = (designerName, index) switch
            {
                var (name, _) when name.Contains("bayonet") => "weapon_knife",
                ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
                ("weapon_hkp2000", 61) => "weapon_usp_silencer",
                ("weapon_deagle", 64) => "weapon_revolver",
                // Only a subset of skin-swapped variants are remapped; other variants use the base designer name.
                _ => designerName
            };

            return designerName;
        }
        private static IWasdMenuManager? GetMenuManager()
        {
            if (Supermoce.Instance != null && Supermoce.Instance.MenuManager == null)
                Supermoce.Instance.MenuManager = new WasdManager();
            return Supermoce.Instance?.MenuManager;
        }
        public static void CloseMenu(CCSPlayerController? player)
        {
            var manager = GetMenuManager();
            if (manager == null) return;
            manager.CloseMenu(player);
        }
        public static bool HasMenu(CCSPlayerController? player)
        {
            var manager = GetMenuManager();
            if (manager == null) return false;
            return manager.HasMenu(player);
        }
        public static void UpdateMenu(CCSPlayerController? player, ConcurrentBag<(string, string)> items)
        {
            if (player == null) return;

            var manager = GetMenuManager();
            if (manager == null) return;

            var playerInfo = GetPlayerInfo(player);
            if (playerInfo == null) return;

            Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> list = [];
            foreach (var item in items)
                list.TryAdd(item.Item1, (p, option) =>
                {
                    Supermoce.Instance?.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { item.Item2 }]);
                    manager.CloseMenu(p);
                });

            manager.UpdateActiveMenu(player, list);
        }
        private static (CCSPlayerController player, SkillPlayerInfo playerInfo, SkillInfo skillData, IWasdMenuManager manager, string skillLine, string itemTemplate, string hoverTemplate)? BuildMenuContext(CCSPlayerController? player, Skills? overrideSkill = null)
        {
            if (player?.IsValid != true) return null;
            var playerInfo = GetPlayerInfo(player);
            if (playerInfo == null) return null;
            var skill = overrideSkill ?? playerInfo.Skill;
            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skill);
            if (skillData == null) return null;
            var manager = GetMenuManager();
            if (manager == null) return null;
            string skillLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='white'>{skillData.Description}</font><br/>";
            string itemTemplate = "<font class='fontSize-s' color='#ffbb00'>{0}</font><br/>";
            string hoverTemplate = "<font class='fontSize-s' class='fontWeight-Bold' color='yellow'>[W/S]  [{0}]  [E]</font><br/>";
            return (player, playerInfo, skillData, manager, skillLine, itemTemplate, hoverTemplate);
        }

        public static void CreateTargetingMenu(
            CCSPlayerController? player,
            Func<CCSPlayerController, bool>? enemyFilter = null,
            string? noEnemiesMsg = null,
            Action? onEmpty = null)
        {
            var ctx = BuildMenuContext(player);
            if (ctx == null) return;

            var enemies = GetAliveEnemies(ctx.Value.player)
                .Where(e => enemyFilter == null || enemyFilter(e))
                .ToArray();

            if (enemies.Length == 0)
            {
                if (onEmpty != null)
                {
                    onEmpty();
                    return;
                }
                PrintToChat(ctx.Value.player, noEnemiesMsg ?? "Nie znaleziono gracza o takim ID.", true);
                return;
            }

            IWasdMenu menu = ctx.Value.manager.CreateMenu(ctx.Value.skillLine, ctx.Value.itemTemplate, ctx.Value.hoverTemplate, "");
            foreach (var enemy in enemies)
                menu.Add(enemy.PlayerName, (p, option) =>
                {
                    Supermoce.Instance?.SkillAction(ctx.Value.playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { enemy.Index.ToString() }]);
                    ctx.Value.manager.CloseMenu(p);
                });

            ctx.Value.manager.OpenMainMenu(ctx.Value.player, menu);
        }

        public static void CreateMenu(CCSPlayerController? player, ConcurrentBag<(string, string)> items)
        {
            var ctx = BuildMenuContext(player);
            if (ctx == null) return;

            IWasdMenu menu = ctx.Value.manager.CreateMenu(ctx.Value.skillLine, ctx.Value.itemTemplate, ctx.Value.hoverTemplate, "");
            foreach (var item in items)
                menu.Add(item.Item1, (p, option) =>
                {
                    Supermoce.Instance?.SkillAction(ctx.Value.playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { item.Item2 }]);
                    ctx.Value.manager.CloseMenu(p);
                });

            ctx.Value.manager.OpenMainMenu(ctx.Value.player, menu);
        }

        public static void CreateMenu(CCSPlayerController? player, Skills skill)
        {
            var ctx = BuildMenuContext(player, skill);
            if (ctx == null) return;

            IWasdMenu menu = ctx.Value.manager.CreateMenu(ctx.Value.skillLine, ctx.Value.itemTemplate, ctx.Value.hoverTemplate, "");
            ctx.Value.manager.OpenMainMenu(ctx.Value.player, menu);
        }

        public static bool IsWarmup()
        {
            return Supermoce.Instance?.GameRules?.WarmupPeriod == true;
        }

         public static bool IsFreezetime()
        {
            return Supermoce.Instance?.GameRules?.FreezePeriod == true;
        }

        public static void SetTeamScores(short ctScore, short tScore, RoundEndReason roundEndReason)
        {
            if (Supermoce.Instance == null || Supermoce.Instance.GameRules == null) return;
            UpdateServerTeamScores(ctScore, tScore);
            TerminateRoundFunc.Invoke(Supermoce.Instance.GameRules.Handle, 5f, roundEndReason, 0, 0);
        }

        public static void TerminateRound(CsTeam winnerTeam)
        {
            if (Supermoce.Instance == null || Supermoce.Instance.GameRules == null) return;
            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
            var ctTeam = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.CounterTerrorist);
            var tTeams = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.Terrorist);
            if (ctTeam == null || tTeams == null) return;

            short ctScore = (short)(winnerTeam == CsTeam.CounterTerrorist ? ctTeam.Score + 1 : ctTeam.Score);
            short tScore = (short)(winnerTeam == CsTeam.Terrorist ? tTeams.Score + 1 : tTeams.Score);

            UpdateServerTeamScores(ctScore, tScore);
            TerminateRoundFunc.Invoke(Supermoce.Instance.GameRules.Handle, 5f, winnerTeam == CsTeam.CounterTerrorist ? RoundEndReason.BombDefused : RoundEndReason.TargetBombed, 0, 0);
        }

        private static void UpdateServerTeamScores(short ctScore, short tScore)
        {
            if (Supermoce.Instance == null || Supermoce.Instance.GameRules == null) return;
            int totalRoundsPlayed = ctScore + tScore;
            int maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 24;
            int halfRounds = maxRounds / 2;
            int overtimeMaxRounds = ConVar.Find("mp_overtime_maxrounds")?.GetPrimitiveValue<int>() ?? 6;
            int overtimeLimit = ConVar.Find("mp_overtime_limit")?.GetPrimitiveValue<int>() ?? 1;

            var gameRulesProxy = Supermoce.Instance.GameRules;
            gameRulesProxy.TotalRoundsPlayed = totalRoundsPlayed;
            gameRulesProxy.ITotalRoundsPlayed = totalRoundsPlayed;
            gameRulesProxy.RoundsPlayedThisPhase = totalRoundsPlayed;

            gameRulesProxy.TeamIntroPeriod = false;
            if (gameRulesProxy.GamePhase == 1 && totalRoundsPlayed < halfRounds)
            {
                gameRulesProxy.GamePhase = 0;
                gameRulesProxy.SwapTeamsOnRestart = true;
                gameRulesProxy.SwitchingTeamsAtRoundReset = true;
                gameRulesProxy.RoundsPlayedThisPhase = 0;
                gameRulesProxy.TeamIntroPeriod = true;
            }

            if (totalRoundsPlayed < halfRounds)
                gameRulesProxy.GamePhase = 0;
            else if (gameRulesProxy.GamePhase == 0)
            {
                gameRulesProxy.GamePhase = 1;
                gameRulesProxy.SwapTeamsOnRestart = true;
                gameRulesProxy.SwitchingTeamsAtRoundReset = true;
                gameRulesProxy.RoundsPlayedThisPhase = 0;
                gameRulesProxy.TeamIntroPeriod = true;
            }

            var structOffset = Supermoce.Instance.GameRules.Handle + Schema.GetSchemaOffset("CCSGameRules", "m_bMapHasBombZone") + 0x02; // HACK: MCCSMatch lives at m_bMapHasBombZone+0x02 in memory — may break on game update
            var matchStruct = Marshal.PtrToStructure<MCCSMatch>(structOffset);

            matchStruct.m_totalScore = (short)totalRoundsPlayed;
            matchStruct.m_actualRoundsPlayed = (short)totalRoundsPlayed;
            gameRulesProxy.MatchInfoDecidedTime = Server.CurrentTime;

            matchStruct.m_ctScoreTotal = ctScore;
            gameRulesProxy.AccountCT = ctScore;
            matchStruct.m_terroristScoreTotal = tScore;
            gameRulesProxy.AccountTerrorist = tScore;

            if (gameRulesProxy.GamePhase == 0)
            {
                matchStruct.m_ctScoreFirstHalf = ctScore;
                matchStruct.m_terroristScoreFirstHalf = tScore;
            }
            else
            {
                matchStruct.m_ctScoreSecondHalf = ctScore;
                matchStruct.m_terroristScoreSecondHalf = tScore;
            }

            if (totalRoundsPlayed >= maxRounds)
            {
                if (gameRulesProxy.OvertimePlaying == 0)
                {
                    gameRulesProxy.OvertimePlaying = 1;
                    gameRulesProxy.SwapTeamsOnRestart = true;
                    gameRulesProxy.SwitchingTeamsAtRoundReset = true;
                }
                else
                {
                    int roundsInOvertime = totalRoundsPlayed - maxRounds;
                    if (roundsInOvertime % overtimeMaxRounds == 0)
                    {
                        int currentOvertime = roundsInOvertime / overtimeMaxRounds;
                        if ( currentOvertime < overtimeLimit)
                        {
                            gameRulesProxy.SwapTeamsOnRestart = true;
                            gameRulesProxy.SwitchingTeamsAtRoundReset = true;
                        }
                    }
                }
            }
            Marshal.StructureToPtr(matchStruct, structOffset, true);
            UpdateClientTeamScores(matchStruct);
        }

        private static void UpdateClientTeamScores(MCCSMatch match)
        {
            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
            var ctTeam = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.CounterTerrorist);
            var tTeams = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.Terrorist);

            if (ctTeam != null && tTeams != null)
            {
                ctTeam.Score = match.m_ctScoreTotal;
                ctTeam.ScoreFirstHalf = match.m_ctScoreFirstHalf;
                ctTeam.ScoreSecondHalf = match.m_ctScoreSecondHalf;
                ctTeam.ScoreOvertime = match.m_ctScoreOvertime;
                Utilities.SetStateChanged(ctTeam, "CTeam", "m_iScore");
                Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreFirstHalf");
                Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreSecondHalf");
                Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreOvertime");

                tTeams.Score = match.m_terroristScoreTotal;
                tTeams.ScoreFirstHalf = match.m_terroristScoreFirstHalf;
                tTeams.ScoreSecondHalf = match.m_terroristScoreSecondHalf;
                tTeams.ScoreOvertime = match.m_terroristScoreOvertime;
                Utilities.SetStateChanged(tTeams, "CTeam", "m_iScore");
                Utilities.SetStateChanged(tTeams, "CCSTeam", "m_scoreFirstHalf");
                Utilities.SetStateChanged(tTeams, "CCSTeam", "m_scoreSecondHalf");
                Utilities.SetStateChanged(tTeams, "CCSTeam", "m_scoreOvertime");
            }
        }

        public static CCSPlayerController[] GetAliveEnemies(CCSPlayerController player)
        {
            return CachedPlayers
                .Where(p => p.PawnIsAlive && p.Team != player.Team && p.IsValid
                         && p.Team != CsTeam.Spectator && p.Team != CsTeam.None)
                .ToArray();
        }

        public static CCSPlayerController? GetEnemyFromCommand(CCSPlayerController player, string[] commands)
        {
            if (commands.Length == 0) return null;
            return Utilities.GetPlayers()
                .FirstOrDefault(p => p.Team != player.Team && p.Index.ToString() == commands[0]);
        }

        public static bool ValidateSkillUse(CCSPlayerController player, Skills skill, out SkillPlayerInfo? playerInfo, string? alreadyUsedMsg = null)
        {
            playerInfo = null;
            if (player == null || !player.IsValid || !player.PawnIsAlive) return false;

            playerInfo = GetPlayerInfo(player);
            if (playerInfo?.Skill != skill) return false;

            if (playerInfo.SkillChance == 1)
            {
                PrintToChat(player, alreadyUsedMsg ?? "Nie posiadasz już tej supermocy", true);
                return false;
            }
            return true;
        }

        public static bool ValidateEnemyTarget(CCSPlayerController player, out CCSPlayerController? enemy, string[] commands, string? notFoundMsg = null)
        {
            enemy = GetEnemyFromCommand(player, commands);
            if (enemy == null || !enemy.IsValid || enemy.PlayerPawn.Value == null || !enemy.PlayerPawn.Value.IsValid)
            {
                PrintToChat(player, notFoundMsg ?? "Nie znaleziono gracza o takim ID.", true);
                return false;
            }
            return true;
        }

        public static bool TryGetTargetFromCommand(CCSPlayerController player, Skills skill, string[] commands,
            out SkillPlayerInfo? playerInfo, out CCSPlayerController? enemy,
            string? alreadyUsedMsg = null, string? notFoundMsg = null)
        {
            playerInfo = null;
            enemy = null;

            if (!ValidateSkillUse(player, skill, out playerInfo, alreadyUsedMsg))
                return false;

            if (!ValidateEnemyTarget(player, out enemy, commands, notFoundMsg))
                return false;

            return true;
        }

        public static void InitTargetingSkill(CCSPlayerController player, Skills skill, string? noEnemiesMsg = null)
        {
            var playerInfo = GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;

            CreateTargetingMenu(player, null, noEnemiesMsg);
        }

        public static void DestroyTargetingSkill(CCSPlayerController player, Action<CCSPlayerController>? extraCleanup = null)
        {
            if (player == null || !player.IsValid) return;
            CloseMenu(player);
            extraCleanup?.Invoke(player);
        }

        public static void UpdateTargetingMenu(CCSPlayerController player, Skills skill)
        {
            if (!HasMenu(player)) return;
            var playerInfo = GetPlayerInfo(player);
            if (playerInfo == null || playerInfo.Skill != skill) return;

            var enemies = GetAliveEnemies(player);
            ConcurrentBag<(string, string)> menuItems = [.. enemies.Select(e => (e.PlayerName, e.Index.ToString()))];
            UpdateMenu(player, menuItems);
        }

        public static void ResetPlayerMovement(CCSPlayerController player)
        {
            var pawn = player?.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;
            pawn.VelocityModifier = 1f;
            pawn.ActualGravityScale = 1f;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MCCSMatch
    {
        public short m_totalScore;
        public short m_actualRoundsPlayed;
        public short m_nOvertimePlaying;
        public short m_ctScoreFirstHalf;
        public short m_ctScoreSecondHalf;
        public short m_ctScoreOvertime;
        public short m_ctScoreTotal;
        public short m_terroristScoreFirstHalf;
        public short m_terroristScoreSecondHalf;
        public short m_terroristScoreOvertime;
        public short m_terroristScoreTotal;
        public short unknown;
        public int m_phase;
    }
}