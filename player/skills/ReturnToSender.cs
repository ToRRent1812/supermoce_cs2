using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ReturnToSender : ISkill
    {
        private const Skills skillName = Skills.ReturnToSender;
                private static readonly HashSet<nint> playersToSender = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                playersToSender.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                var attacker = @event.Attacker;
                var victim = @event.Userid;
                int damage = @event.DmgHealth;

                if (attacker == null || victim == null || !Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return HookResult.Continue;
                var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
                if (attackerInfo == null || attackerInfo.Skill != skillName) return HookResult.Continue;

                if (playersToSender.TryGetValue(victim!.Handle, out _))
                    return HookResult.Continue;

                var spawn = GetSpawnVector(victim);
                if (spawn == null) return HookResult.Continue;
                victim!.PlayerPawn!.Value!.Teleport(spawn);
                playersToSender.Add(victim.Handle);
                return HookResult.Stop;
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            playersToSender.Remove(player.Handle);
        }

        private static Vector? GetSpawnVector(CCSPlayerController player)
        {
            var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(player.Team == CsTeam.Terrorist ? "info_player_terrorist" : "info_player_counterterrorist").ToList();
            if (spawns.Count != 0)
            {
                var randomSpawn = spawns[Instance.Random.Next(spawns.Count)];
                return randomSpawn.AbsOrigin!;
            }
            return null;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#a68132", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}