using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Rubber: ISkill
    {
        private const Skills skillName = Skills.Rubber;

        private static readonly ConcurrentDictionary<CCSPlayerPawn, float> playersToSlow = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Gumowe Kule", "Twoje pociski spowalniają graczy", "#8B4513");
        }

        public static void NewRound()
        {
            playersToSlow.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            var victimPawn = victim!.PlayerPawn.Value;
            if (victimPawn == null || !victimPawn.IsValid) return;

            var rubberTime = 2f;
            if (attackerInfo?.Skill == skillName)
                playersToSlow.AddOrUpdate(victimPawn, Server.TickCount + (64 * rubberTime), (k, v) => Server.TickCount + (64 * rubberTime));
        }

        public static void OnTick()
        {
            foreach(var item in playersToSlow)
            {
                var pawn = item.Key;
                var time = item.Value;
                if (time >= Server.TickCount)
                    ChangeVelocity(pawn);
                else
                    playersToSlow.TryRemove(item.Key, out _);
            }
        }

        private static void ChangeVelocity(CCSPlayerPawn pawn)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.VelocityModifier = .2f;
        }
    }
}