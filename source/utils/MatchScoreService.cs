using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;

namespace Supermoce
{
    public static class MatchScoreService
    {
        private const int MatchStructDelta = 0x02; // MCCSMatch sits at m_bMapHasBombZone+0x02
        private static int _matchOffset = int.MinValue;

        private static int MatchOffset()
        {
            if (_matchOffset != int.MinValue) return _matchOffset;

            try
            {
                int bombZone = Schema.GetSchemaOffset("CCSGameRules", "m_bMapHasBombZone");
                _matchOffset = bombZone > 0 ? bombZone + MatchStructDelta : 0;
            }
            catch
            {
                _matchOffset = 0;
            }

            if (_matchOffset == 0)
                Server.PrintToConsole("[Supermoce] MCCSMatch offset unavailable; using typed team scores only.");

            return _matchOffset;
        }

        public static void TerminateRound(CsTeam winnerTeam)
        {
            var rules = Utilities.GetGameRules();
            if (rules == null || rules.Handle == IntPtr.Zero) return;

            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").ToList();
            var ctTeam = teams.FirstOrDefault(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.CounterTerrorist);
            var tTeam = teams.FirstOrDefault(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.Terrorist);
            if (ctTeam == null || tTeam == null) return;

            short ctScore = (short)(winnerTeam == CsTeam.CounterTerrorist ? ctTeam.Score + 1 : ctTeam.Score);
            short tScore = (short)(winnerTeam == CsTeam.Terrorist ? tTeam.Score + 1 : tTeam.Score);

            UpdateServerTeamScores(ctScore, tScore);
            rules.TerminateRound(5f, winnerTeam == CsTeam.CounterTerrorist
                ? RoundEndReason.BombDefused
                : RoundEndReason.TargetBombed);
        }

        private static void UpdateServerTeamScores(short ctScore, short tScore)
        {
            var rules = Utilities.GetGameRules();
            if (rules == null || rules.Handle == IntPtr.Zero) return;

            int totalRoundsPlayed = ctScore + tScore;
            int maxRounds = ConVar<int>.Find("mp_maxrounds")?.Value ?? 24;
            int halfRounds = maxRounds / 2;
            int overtimeMaxRounds = ConVar<int>.Find("mp_overtime_maxrounds")?.Value ?? 6;
            int overtimeLimit = ConVar<int>.Find("mp_overtime_limit")?.Value ?? 1;

            rules.TotalRoundsPlayed = totalRoundsPlayed;
            rules.ITotalRoundsPlayed = totalRoundsPlayed;
            rules.RoundsPlayedThisPhase = totalRoundsPlayed;

            rules.TeamIntroPeriod = false;
            if (rules.GamePhase == 1 && totalRoundsPlayed < halfRounds)
            {
                rules.GamePhase = 0;
                rules.SwapTeamsOnRestart = true;
                rules.SwitchingTeamsAtRoundReset = true;
                rules.RoundsPlayedThisPhase = 0;
                rules.TeamIntroPeriod = true;
            }

            if (totalRoundsPlayed < halfRounds)
                rules.GamePhase = 0;
            else if (rules.GamePhase == 0)
            {
                rules.GamePhase = 1;
                rules.SwapTeamsOnRestart = true;
                rules.SwitchingTeamsAtRoundReset = true;
                rules.RoundsPlayedThisPhase = 0;
                rules.TeamIntroPeriod = true;
            }

            rules.MatchInfoDecidedTime = Server.CurrentTime;

            MCCSMatch? match = null;
            int offset = MatchOffset();
            if (offset > 0)
            {
                try
                {
                    var matchStruct = Marshal.PtrToStructure<MCCSMatch>(rules.Handle + offset);

                    matchStruct.m_totalScore = (short)totalRoundsPlayed;
                    matchStruct.m_actualRoundsPlayed = (short)totalRoundsPlayed;
                    matchStruct.m_ctScoreTotal = ctScore;
                    matchStruct.m_terroristScoreTotal = tScore;

                    if (rules.GamePhase == 0)
                    {
                        matchStruct.m_ctScoreFirstHalf = ctScore;
                        matchStruct.m_terroristScoreFirstHalf = tScore;
                    }
                    else
                    {
                        matchStruct.m_ctScoreSecondHalf = ctScore;
                        matchStruct.m_terroristScoreSecondHalf = tScore;
                    }

                    match = matchStruct;
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[Supermoce] MCCSMatch write failed: {ex.Message}");
                }
            }

            rules.AccountCT = ctScore;
            rules.AccountTerrorist = tScore;

            if (totalRoundsPlayed >= maxRounds)
            {
                if (rules.OvertimePlaying == 0)
                {
                    rules.OvertimePlaying = 1;
                    rules.SwapTeamsOnRestart = true;
                    rules.SwitchingTeamsAtRoundReset = true;
                }
                else
                {
                    int roundsInOvertime = totalRoundsPlayed - maxRounds;
                    if (roundsInOvertime % overtimeMaxRounds == 0)
                    {
                        int currentOvertime = roundsInOvertime / overtimeMaxRounds;
                        if (currentOvertime < overtimeLimit)
                        {
                            rules.SwapTeamsOnRestart = true;
                            rules.SwitchingTeamsAtRoundReset = true;
                        }
                    }
                }
            }

            if (match is { } finalMatch)
                Marshal.StructureToPtr(finalMatch, rules.Handle + offset, true);

            UpdateClientTeamScores(rules, match, ctScore, tScore);
        }

        private static void UpdateClientTeamScores(CCSGameRules rules, MCCSMatch? match, short ctScore, short tScore)
        {
            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").ToList();
            var ctTeam = teams.FirstOrDefault(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.CounterTerrorist);
            var tTeam = teams.FirstOrDefault(t => t.IsValid && (CsTeam)t.TeamNum == CsTeam.Terrorist);
            if (ctTeam == null || tTeam == null) return;

            bool firstHalf = rules.GamePhase == 0;
            int ctFirst = match?.m_ctScoreFirstHalf ?? (firstHalf ? ctScore : 0);
            int ctSecond = match?.m_ctScoreSecondHalf ?? (firstHalf ? 0 : ctScore);
            int tFirst = match?.m_terroristScoreFirstHalf ?? (firstHalf ? tScore : 0);
            int tSecond = match?.m_terroristScoreSecondHalf ?? (firstHalf ? 0 : tScore);

            ctTeam.Score = ctScore;
            ctTeam.ScoreFirstHalf = ctFirst;
            ctTeam.ScoreSecondHalf = ctSecond;
            ctTeam.ScoreOvertime = match?.m_ctScoreOvertime ?? ctTeam.ScoreOvertime;
            Utilities.SetStateChanged(ctTeam, "CTeam", "m_iScore");
            Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreFirstHalf");
            Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreSecondHalf");
            Utilities.SetStateChanged(ctTeam, "CCSTeam", "m_scoreOvertime");

            tTeam.Score = tScore;
            tTeam.ScoreFirstHalf = tFirst;
            tTeam.ScoreSecondHalf = tSecond;
            tTeam.ScoreOvertime = match?.m_terroristScoreOvertime ?? tTeam.ScoreOvertime;
            Utilities.SetStateChanged(tTeam, "CTeam", "m_iScore");
            Utilities.SetStateChanged(tTeam, "CCSTeam", "m_scoreFirstHalf");
            Utilities.SetStateChanged(tTeam, "CCSTeam", "m_scoreSecondHalf");
            Utilities.SetStateChanged(tTeam, "CCSTeam", "m_scoreOvertime");
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
