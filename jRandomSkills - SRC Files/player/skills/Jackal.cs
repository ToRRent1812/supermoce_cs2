using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Jackal : ISkill
    {
        private const Skills skillName = Skills.Jackal;
        private static readonly ConcurrentDictionary<ulong, byte> playersInAction = [];
        private static readonly ConcurrentDictionary<CCSPlayerController, CParticleSystem?> playersStep = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Stópkarz", "Przeciwnicy zostawiają po sobie ślady.", "#f542ef");
            Instance?.AddToManifest("particles/ui/hud/ui_map_def_utility_trail.vpcf");
        }

        public static void NewRound()
        {
            foreach (var step in playersStep.Values)
                if (step != null && step.IsValid)
                    step.AcceptInput("Kill");
            playersStep.Clear();
            playersInAction.Clear();
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach( var (info, player) in infoList)
            {
                if (player == null || !player.IsValid) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
                bool isObservingJackal = false;

                if (targetHandle != nint.Zero)
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == targetHandle);
                    var targetInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == target?.SteamID);
                    if (targetInfo?.Skill == skillName) isObservingJackal = true;
                }

                bool hasSkill = playerInfo?.Skill == skillName || isObservingJackal;

                foreach (var param in playersStep)
                {
                    var enemy = param.Key;
                    var step = param.Value;
                    if (step == null || !step.IsValid) continue;

                    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)step.Index);
                    if (entity == null || !entity.IsValid) continue;

                    if (!hasSkill || enemy.Team == player.Team)
                        info.TransmitEntities.Remove(entity.Index);
                }
            }
        }

        public static void CreatePlayerTrail(CCSPlayerController? player)
        {
            if (player == null) return;
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (!playersStep.ContainsKey(player)) return;

            CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
            if (particle == null) return;

            particle.EffectName = "particles/ui/hud/ui_map_def_utility_trail.vpcf";
            particle.StartActive = true;

            particle.Teleport(playerPawn.AbsOrigin);
            particle.DispatchSpawn();

            // Parent the particle to the player's pawn so it follows them correctly
            particle.AcceptInput("SetParent", playerPawn, playerPawn, "!activator");
            particle.AcceptInput("Start");

            playersStep.AddOrUpdate(player, particle, (k, v) => particle);

            Instance?.AddTimer(5.0f, () => {
                if (particle != null && particle.IsValid)
                    particle.AcceptInput("Kill");
                CreatePlayerTrail(player);
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            Event.EnableTransmit();
            playersInAction.TryAdd(player.SteamID, 0);
            foreach (var _player in Utilities.GetPlayers().Where(p => p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.PawnIsAlive && p.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist))
            {
                if (!playersStep.ContainsKey(_player))
                    playersStep.TryAdd(_player, null);
                CreatePlayerTrail(_player);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersStep.TryRemove(player, out _);
            playersInAction.TryRemove(player.SteamID, out _);
            if (playersInAction.IsEmpty)
                NewRound();
        }
    }
}