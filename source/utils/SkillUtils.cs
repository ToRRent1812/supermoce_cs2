using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using System.Globalization;

namespace Supermoce
{
    public static class SkillUtils
    {
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> HEGrenadeProjectile_CreateFunc = new(GameData.GetSignature("HEGrenadeProjectile_CreateFunc"));
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CSmokeGrenadeProjectile> SmokeGrenadeProjectile_CreateFunc = new(GameData.GetSignature("SmokeGrenadeProjectile_CreateFunc"));
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, int, CFlashbangProjectile> FlashbangProjectile_CreateFunc = new(GameData.GetSignature("FlashbangProjectile_CreateFunc"));
        public static CCSPlayerController[] CachedPlayers { get; private set; } = [];
        public static CCSPlayerController[] CachedHumans { get; private set; } = [];
        public static int CurrentTick { get; private set; }
        private static SkillPlayerInfo[] _cachedSkillSnapshot = [];
        private static readonly ConcurrentDictionary<int, SkillPlayerInfo> _playerInfoCache = [];
        public static CCSPlayerController[] AllPlayers()
        {
            var list = new List<CCSPlayerController>(Server.MaxPlayers);
            for (int slot = 0; slot < Server.MaxPlayers; slot++)
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player == null || !player.IsValid || player.IsHLTV) continue;
                list.Add(player);
            }
            return list.ToArray();
        }

        public static void RefreshTickState()
        {
            CurrentTick = Server.TickCount;
            var players = AllPlayers();
            if (players.Length == CachedPlayers.Length)
            {
                bool same = true;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].Slot != CachedPlayers[i].Slot || players[i].IsBot != CachedPlayers[i].IsBot) { same = false; break; }
                }
                if (same) return;
            }
            CachedPlayers = players;
            CachedHumans = players.Where(p => !p.IsBot).ToArray();
            _cachedSkillSnapshot = Supermoce.Instance?.SkillPlayer?.Values.ToArray() ?? [];
            _playerInfoCache.Clear();
            foreach (var sp in _cachedSkillSnapshot)
                _playerInfoCache.TryAdd(sp.Slot, sp);
        }

        public static void ForceRefresh()
        {
            CachedPlayers = [];
            CachedHumans = [];
        }
        public static SkillPlayerInfo? GetPlayerInfo(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid) return null;
            _playerInfoCache.TryGetValue(player.Slot, out var info);
            return info;
        }
        public static SkillPlayerInfo[] GetSkillSnapshot() => _cachedSkillSnapshot;
        private static string ChatIcon(bool isError) => isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}►{ChatColors.Lime}";

        public static void PrintToChat(CCSPlayerController player, string msg, bool isError = false)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
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
                var info = new SkillInfo(skill, name, desc, color, teamnum, objective);
                SkillData.Skills.Add(info);
                SkillData.BySkill[skill] = info;
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
            int cooldownStep = 5)
        {
            RegisterSkill(skill, name, description, color, teamnum, objective);
            var config = new ActiveSkillConfig
            {
                MinCooldown = minCooldown,
                MaxCooldown = maxCooldown,
                CooldownStep = cooldownStep,
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
        public static void CreateSmokeGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
        {
            SmokeGrenadeProjectile_CreateFunc.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, 45, teamNum);
        }
        public static void CreateFlashGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
        {
            FlashbangProjectile_CreateFunc.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, 46, teamNum);
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
                    pawn?.CommitSuicide(false, true);
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
                _ => designerName
            };

            return designerName;
        }
        public static void CloseMenu(CCSPlayerController? player) => SkillMenuPanel.Close(player);

        public static bool HasMenu(CCSPlayerController? player) => SkillMenuPanel.HasOpen(player);

        public static void CreateTargetingMenu(
            CCSPlayerController? player,
            Func<CCSPlayerController, bool>? enemyFilter = null,
            string? noEnemiesMsg = null,
            Action? onEmpty = null)
        {
            if (player?.IsValid != true) return;
            var playerInfo = GetPlayerInfo(player);
            var skillData = playerInfo == null ? null : SkillData.Find(playerInfo.Skill);
            if (playerInfo == null || skillData == null) return;

            var enemies = GetAliveEnemies(player)
                .Where(e => enemyFilter == null || enemyFilter(e))
                .ToArray();

            if (enemies.Length == 0)
            {
                if (onEmpty != null)
                {
                    onEmpty();
                    return;
                }
                PrintToChat(player, noEnemiesMsg ?? "Nie znaleziono gracza o takim ID.", true);
                return;
            }

            if (player.IsBot)
            {
                var enemy = enemies[Supermoce.Instance?.Random.Next(enemies.Length) ?? 0];
                Supermoce.Instance?.SkillAction(skillData.Skill.ToString(), "TypeSkill", [player, new[] { enemy.Index.ToString() }]);
                return;
            }

            OpenMenu(player, playerInfo, skillData, enemies.Select(e => (e.PlayerName, e.Index.ToString())));
        }

        public static void CreateMenu(CCSPlayerController? player, ConcurrentBag<(string, string)> items)
        {
            if (player?.IsValid != true) return;
            var playerInfo = GetPlayerInfo(player);
            var skillData = playerInfo == null ? null : SkillData.Find(playerInfo.Skill);
            if (playerInfo == null || skillData == null) return;

            if (player.IsBot)
            {
                var options = items.ToArray();
                if (options.Length > 0)
                {
                    var pick = options[Supermoce.Instance?.Random.Next(options.Length) ?? 0];
                    Supermoce.Instance?.SkillAction(skillData.Skill.ToString(), "TypeSkill", [player, new[] { pick.Item2 }]);
                }
                return;
            }

            OpenMenu(player, playerInfo, skillData, items);
        }

        public static void CreateMenu(CCSPlayerController? player, Skills skill)
        {
            if (player?.IsValid != true) return;
            var playerInfo = GetPlayerInfo(player);
            var skillData = SkillData.Find(skill);
            if (playerInfo == null || skillData == null) return;

            if (player.IsBot) return;

            OpenMenu(player, playerInfo, skillData, []);
        }

        public static void UpdateMenu(CCSPlayerController? player, ConcurrentBag<(string, string)> items)
        {
            if (player == null) return;
            SkillMenuPanel.Update(player, items.Select(i => new MenuEntry(i.Item1, i.Item2)));
        }

        private static void OpenMenu(
            CCSPlayerController player,
            SkillPlayerInfo playerInfo,
            SkillInfo skillData,
            IEnumerable<(string Label, string Payload)> items)
        {
            var skill = playerInfo.Skill;
            SkillMenuPanel.Show(
                player,
                skillData.Name,
                skillData.Description,
                items.Select(i => new MenuEntry(i.Label, i.Payload)),
                (p, payload) =>
                {
                    Supermoce.Instance?.SkillAction(skill.ToString(), "TypeSkill", [p, new[] { payload }]);
                    CloseMenu(p);
                });
        }

        public static bool IsWarmup()
        {
            return GameState.Warmup;
        }

         public static bool IsFreezetime()
        {
            return GameState.FreezePeriod;
        }

        public static void TerminateRound(CsTeam winnerTeam) =>
            MatchScoreService.TerminateRound(winnerTeam);

        public static void AwardRoundEndMoney(CsTeam winnerTeam, CCSPlayerController? mvpPlayer = null, RoundEndReason reason = RoundEndReason.BombDefused)
        {
            int maxMoney = ConVar<int>.Find("mp_maxmoney")?.Value ?? 16000;

            int teamReward = reason switch
            {
                RoundEndReason.BombDefused => ConVar<int>.Find("cash_team_win_by_defusing_bomb")?.Value ?? 3500,
                RoundEndReason.TargetBombed => ConVar<int>.Find("cash_team_win_by_bomb")?.Value ?? 3500,
                RoundEndReason.AllHostageRescued => ConVar<int>.Find("cash_team_win_by_hostage_rescue")?.Value ?? 3500,
                _ => ConVar<int>.Find("cash_team_elimination_bomb_map")?.Value ?? 3250,
            };

            int individualBonus = reason switch
            {
                RoundEndReason.BombDefused => ConVar<int>.Find("cash_player_bomb_defused")?.Value ?? 300,
                RoundEndReason.TargetBombed => ConVar<int>.Find("cash_player_bomb_planted")?.Value ?? 300,
                RoundEndReason.AllHostageRescued => ConVar<int>.Find("cash_player_rescued_hostage")?.Value ?? 600,
                _ => 0,
            };

            int loserBonus = ConVar<int>.Find("cash_team_loser_bonus")?.Value ?? 2400;

            CsTeam loserTeam = winnerTeam == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;

            foreach (var player in CachedPlayers)
            {
                if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
                    continue;

                var moneyServices = player.InGameMoneyServices;
                if (moneyServices == null) continue;

                int currentMoney = moneyServices.Account;

                if (player.Team == winnerTeam)
                {
                    currentMoney += teamReward;
                    if (mvpPlayer != null && player.Slot == mvpPlayer.Slot)
                        currentMoney += individualBonus;
                }
                else if (player.Team == loserTeam)
                    currentMoney += loserBonus;

                moneyServices.Account = Math.Min(currentMoney, maxMoney);
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
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
            return CachedPlayers
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

}
