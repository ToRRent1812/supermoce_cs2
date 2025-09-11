using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Muhammed : ISkill
    {
        private const Skills skillName = Skills.Muhammed;
        private static readonly float explosionRadius = Config.GetValue<float>(skillName, "explosionRadius");
        private static readonly int explosionDamage = Config.GetValue<int>(skillName, "explosionDamage");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !IsDeadPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
                if (playerInfo?.Skill == skillName)
                    SpawnExplosion(player!);

                return HookResult.Continue;
            });
        }

        private static void SpawnExplosion(CCSPlayerController player)
        {
            var heProjectile = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");
            if (heProjectile == null || !heProjectile.IsValid) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null) return;

            Vector pos = pawn.AbsOrigin;
            pos.Z += 10;

            heProjectile.TicksAtZeroVelocity = 100;
            heProjectile.TeamNum = player.TeamNum;
            heProjectile.Damage = explosionDamage;
            heProjectile.DmgRadius = (int)explosionRadius;
            heProjectile.Teleport(pos, null, new Vector(0, 0, -10));
            heProjectile.DispatchSpawn();
            heProjectile.AcceptInput("InitializeSpawnFromWorld", player.PlayerPawn.Value, player.PlayerPawn.Value, "");
            heProjectile.DetonateTime = 0;

            var fileNames = new[] { "radiobotfallback01", "radiobotfallback02", "radiobotfallback04" };
            Instance.AddTimer(0.1f, () =>
            {
                var randomFile = fileNames[new Random().Next(fileNames.Length)];
                player.ExecuteClientCommand($"play vo/agents/balkan/{randomFile}.vsnd");
            });
        }

        private static bool IsDeadPlayerValid(CCSPlayerController? player)
        {
            return player != null && player.IsValid && player.PlayerPawn?.Value != null;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#F5CB42", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float explosionRadius = 900.0f, int explosionDamage = 500) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ExplosionRadius { get; set; } = explosionRadius;
            public int ExplosionDamage { get; set; } = explosionDamage;
        }
    }
}