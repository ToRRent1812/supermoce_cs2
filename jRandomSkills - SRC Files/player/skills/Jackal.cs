using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Jackal : ISkill
    {
        private const Skills skillName = Skills.Jackal;
        private static readonly int maxStepBeam = Config.GetValue<int>(skillName, "maxStepBeam");
        private static readonly Dictionary<CCSPlayerController, List<CBeam>> stepBeams = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            foreach (var beams in stepBeams.Values)
                foreach (var beam in beams)
                    if (beam != null && beam.IsValid)
                        beam.AcceptInput("Kill");
            stepBeams.Clear();
        }

        public static void OnTick()
        {
            if (Server.TickCount % 8 != 0) return;
            foreach (var step in stepBeams.ToList())
            {
                var player = step.Key;
                if (!player.IsValid)
                {
                    stepBeams.Remove(player);
                    continue;
                }

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null) continue;

                var beams = step.Value;
                Vector lastBeamVector = (beams != null && beams.Count > 0 && beams.Last() != null)
                    ? beams.Last().EndPos : pawn.AbsOrigin;

                var newBeam = CreateBeamStep(step.Key.Team, lastBeamVector, pawn.AbsOrigin);
                if (newBeam != null)
                    step.Value.Add(newBeam);

                if (beams?.Count >= maxStepBeam)
                {
                    beams[0].AcceptInput("Kill");
                    step.Value.RemoveAt(0);
                }
            }
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach( var (info, player) in infoList)
            {
                if (player == null) continue;
                foreach (var step in stepBeams)
                {
                    var enemy = step.Key;
                    var beams = step.Value;

                    var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == player?.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle);
                    var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    var observerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

                    foreach (var beam in beams)
                        if (playerInfo?.Skill != skillName && observerInfo?.Skill != skillName)
                            info.TransmitEntities.Remove(beam);
                        else if (enemy.Team == player.Team)
                            info.TransmitEntities.Remove(beam);
                }
            }
        }

        public static CBeam? CreateBeamStep(CsTeam team, Vector start, Vector stop)
        {
            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
            if (beam == null) return null;

            beam.DispatchSpawn();
            if (!beam.IsValid) return null;

            beam.Render = team == CsTeam.Terrorist ? Color.FromArgb(100, 255, 165, 0) : Color.FromArgb(100, 173, 216, 230);
            beam.Width = 2.0f;
            beam.EndWidth = 2.0f;
            beam.Teleport(start);

            beam.EndPos.X = stop.X;
            beam.EndPos.Y = stop.Y;
            beam.EndPos.Z = stop.Z;

            return beam;
        }

        public static void EnableSkill(CCSPlayerController _)
        {
            SkillUtils.EnableTransmit();
            foreach (var _player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.Team != CsTeam.Spectator))
                if (!stepBeams.ContainsKey(_player))
                    stepBeams.Add(_player, []);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            playerInfo.Skill = Skills.None;
            if (!Instance.SkillPlayer.Any(s => s.Skill == skillName))
                NewRound();
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#f542ef", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int maxStepBeam = 100) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int MaxStepBeam { get; set; } = maxStepBeam;
        }
    }
}