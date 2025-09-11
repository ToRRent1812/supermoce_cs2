using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Runtime.InteropServices;

namespace jRandomSkills
{
    public static class SkillUtils
    {
        private static readonly MemoryFunctionVoid<nint, float, RoundEndReason, nint, nint> TerminateRoundFunc = new(GameData.GetSignature("CCSGameRules_TerminateRound"));

        public static void PrintToChat(CCSPlayerController player, string msg, bool isError)
        {
            string checkIcon = isError ? $"{ChatColors.DarkRed}✖{ChatColors.LightRed}" : $"{ChatColors.Green}✔{ChatColors.Lime}";
            player.PrintToChat($" {ChatColors.DarkRed}► {ChatColors.Green} {checkIcon} {msg}");
        }

        public static void RegisterSkill(Skills skill, string color, bool display = true)
        {
            if (!SkillData.Skills.Any(s => s.Skill == skill))
                SkillData.Skills.Add(new jSkill_SkillInfo(skill, color, display));
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

        public static double GetDistance(Vector vector1, Vector vector2)
        {
            return Math.Sqrt(Math.Pow(vector2.X - vector1.X, 2) + Math.Pow(vector2.Y - vector1.Y, 2) + Math.Pow(vector2.Z - vector1.Z, 2));
        }

        public static string SecondsToTimer(int totalSeconds)
        {
            if (totalSeconds <= 0) return "00:00";
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
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

        public static void TakeHealth(CCSPlayerPawn? pawn, int damage)
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            int newHealth = (int)(pawn.Health - damage);
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
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            int newHealth = (int)(pawn.Health + extraHealth);
            pawn.Health = Math.Min(newHealth, maxHealth);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            pawn.MaxHealth = maxHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        }

        public static string GetDesignerName(CBasePlayerWeapon weapon)
        {
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

        public static bool IsWarmup()
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("cs_gamerules").Single().As<CCSGameRulesProxy>().GameRules!;
            if (gameRules == null) return false;

            return gameRules.WarmupPeriod;
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
