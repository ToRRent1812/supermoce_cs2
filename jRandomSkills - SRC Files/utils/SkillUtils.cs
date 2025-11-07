using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WASDMenuAPI.Classes;
using WASDSharedAPI;

namespace jRandomSkills
{
    public static class SkillUtils
    {
        private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int> HEGrenadeProjectile_CreateFunc = new(GameData.GetSignature("HEGrenadeProjectile_CreateFunc"));
        private static readonly MemoryFunctionVoid<nint, float, RoundEndReason, nint, nint> TerminateRoundFunc = new(GameData.GetSignature("CCSGameRules_TerminateRound"));

        public static void PrintToChat(CCSPlayerController player, string msg, bool isError = false)
        {
            string checkIcon = isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}✔{ChatColors.Lime}";
            player.PrintToChat($" {ChatColors.DarkRed}► {ChatColors.Green} {checkIcon} {msg}");
        }

        public static void PrintToChatAll(string msg, bool isError = false)
        {
            string checkIcon = isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}✔{ChatColors.Lime}";
            Server.PrintToChatAll($" {ChatColors.DarkRed}► {ChatColors.Green} {checkIcon} {msg}");
        }

        public static void RegisterSkill(Skills skill, string name, string desc, string color, byte teamnum = 0)
        {
            if (!SkillData.Skills.Any(s => s.Skill == skill))
                SkillData.Skills.Add(new jSkill_SkillInfo(skill, name, desc, color, teamnum));
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
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.CBodyComponent == null || playerPawn.CBodyComponent.SceneNode == null) return;

            playerPawn.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = scale;
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            Server.NextFrame(() => playerPawn.AcceptInput("SetScale", playerPawn, playerPawn, scale.ToString()));
        }

        public static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
        {
            HEGrenadeProjectile_CreateFunc.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, 44, teamNum);
        }

        public static void TakeHealth(CCSPlayerPawn? pawn, int damage)
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            int newHealth = pawn.Health - damage;
            pawn.Health = newHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            if (pawn.Health <= 0)
                Server.NextFrame(() =>
                {
                    pawn?.CommitSuicide(false, true);
                });
        }

        public static void AddHealth(CCSPlayerPawn? pawn, int extraHealth, int maxHealth = 100)
        {
            if (pawn?.IsValid != true || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

            pawn.Health = Math.Min(pawn.Health + extraHealth, maxHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            pawn.MaxHealth = maxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
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

        private static IWasdMenuManager? GetMenuManager()
        {
            if (jRandomSkills.Instance != null && jRandomSkills.Instance.MenuManager == null)
                jRandomSkills.Instance.MenuManager = new WasdManager();
            return jRandomSkills.Instance?.MenuManager;
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

            var playerInfo = jRandomSkills.Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            Dictionary<string, Action<CCSPlayerController, IWasdMenuOption>> list = [];
            foreach (var item in items)
                list.TryAdd(item.Item1, (p, option) =>
                {
                    jRandomSkills.Instance?.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { item.Item2 }]);
                    manager.CloseMenu(p);
                });

            manager.UpdateActiveMenu(player, list);
        }

         public static void CreateMenu(CCSPlayerController? player, ConcurrentBag<(string, string)> enemies)
        {
            if (player?.IsValid != true) return;

            var playerInfo = jRandomSkills.Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == playerInfo.Skill);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font><br><font class='fontSize-s' class='fontWeight-Bold' color='white'>{skillData.Description}</font>";

            var manager = GetMenuManager();
            if (manager == null) return;

            IWasdMenu menu = manager.CreateMenu(skillLine, "<font class='fontSize-s' color='yellow'>W/S – Przewijanie | ROZBRAJANIE - wybór</font>");
            foreach (var enemy in enemies)
                menu.Add(enemy.Item1, (p, option) =>
                {
                    jRandomSkills.Instance?.SkillAction(playerInfo.Skill.ToString(), "TypeSkill", [p, new[] { enemy.Item2 }]);
                    manager.CloseMenu(p);
                });
            manager.OpenMainMenu(player, menu);
        }

        public static bool IsWarmup()
        {
            return jRandomSkills.Instance?.GameRules?.WarmupPeriod == true;
        }

         public static bool IsFreezetime()
        {
            return jRandomSkills.Instance?.GameRules?.FreezePeriod == true;
        }

        public static void SetTeamScores(short ctScore, short tScore, RoundEndReason roundEndReason)
        {
            if (jRandomSkills.Instance == null || jRandomSkills.Instance.GameRules == null) return;
            UpdateServerTeamScores(ctScore, tScore);
            TerminateRoundFunc.Invoke(jRandomSkills.Instance.GameRules.Handle, 5f, roundEndReason, 0, 0);
        }

        public static void TerminateRound(CsTeam winnerTeam)
        {
            if (jRandomSkills.Instance == null || jRandomSkills.Instance.GameRules == null) return;
            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
            var ctTeam = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.CounterTerrorist);
            var tTeams = teams.First(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.Terrorist);
            if (ctTeam == null || tTeams == null) return;

            short ctScore = (short)(winnerTeam == CsTeam.CounterTerrorist ? ctTeam.Score + 1 : ctTeam.Score);
            short tScore = (short)(winnerTeam == CsTeam.Terrorist ? tTeams.Score + 1 : tTeams.Score);

            UpdateServerTeamScores(ctScore, tScore);
            TerminateRoundFunc.Invoke(jRandomSkills.Instance.GameRules.Handle, 5f, winnerTeam == CsTeam.CounterTerrorist ? RoundEndReason.BombDefused : RoundEndReason.TargetBombed, 0, 0);
        }

        private static void UpdateServerTeamScores(short ctScore, short tScore)
        {
            if (jRandomSkills.Instance == null || jRandomSkills.Instance.GameRules == null) return;
            int totalRoundsPlayed = ctScore + tScore;
            int maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 24;
            int halfRounds = maxRounds / 2;
            int overtimeMaxRounds = ConVar.Find("mp_overtime_maxrounds")?.GetPrimitiveValue<int>() ?? 6;
            int overtimeLimit = ConVar.Find("mp_overtime_limit")?.GetPrimitiveValue<int>() ?? 1;

            var gameRulesProxy = jRandomSkills.Instance.GameRules;
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

            var structOffset = jRandomSkills.Instance.GameRules.Handle + Schema.GetSchemaOffset("CCSGameRules", "m_bMapHasBombZone") + 0x02;
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
            gameRulesProxy.OvertimePlaying = 0;

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