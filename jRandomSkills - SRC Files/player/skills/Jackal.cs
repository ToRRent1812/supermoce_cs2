using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Jackal : ISkill
    {
        private const Skills skillName = Skills.Jackal;
        private static readonly ConcurrentDictionary<ulong, byte> playersInAction = [];
        private static readonly ConcurrentDictionary<CCSPlayerController, ConcurrentQueue<CBeam>> stepBeams = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Stópkarz", "Przeciwnicy zostawiają po sobie ślady.", "#f542ef");
        }

        public static void NewRound()
        {
            foreach (var beams in stepBeams.Values)
                foreach (var beam in beams)
                    if (beam != null && beam.IsValid)
                        beam.AcceptInput("Kill");
            stepBeams.Clear();
            playersInAction.Clear();
        }

        public static void OnTick()
        {
            if (Server.TickCount % 16 != 0) return;
            foreach (var step in stepBeams.ToList())
            {
                var player = step.Key;
                if (!player.IsValid)
                {
                    stepBeams.TryRemove(player, out _);
                    continue;
                }

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null) continue;

                var beams = step.Value;
                Vector lastBeamVector = (beams != null && beams.Count > 0 && beams.Last() != null)
                    ? beams.Last().EndPos : pawn.AbsOrigin;

                var newBeam = CreateBeamStep(step.Key.Team, lastBeamVector, pawn.AbsOrigin);
                if (newBeam != null)
                    step.Value.Enqueue(newBeam);

                if (beams?.Count >= 75)
                {
                    if (beams.TryPeek(out var beam))
                        beam.AcceptInput("Kill");
                    step.Value.TryDequeue(out _);
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
                    var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    var observerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

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

        public static void EnableSkill(CCSPlayerController player)
        {
            Event.EnableTransmit();
            playersInAction.TryAdd(player.SteamID, 0);
            foreach (var _player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.PawnIsAlive && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
                if (!stepBeams.ContainsKey(_player))
                    stepBeams.TryAdd(_player, []);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersInAction.TryRemove(player.SteamID, out _);
            if (playersInAction.IsEmpty)
                NewRound();
        }
    }
}