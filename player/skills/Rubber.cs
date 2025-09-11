using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Rubber: ISkill
    {
        private const Skills skillName = Skills.Rubber;
        private static readonly float rubberTime = Config.GetValue<float>(skillName, "slownessTime");
        private static readonly float rubberModifier = Config.GetValue<float>(skillName, "slownessModifier");

        private static readonly Dictionary<CCSPlayerPawn, float> playersToSlow = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            
            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                var attacker = @event.Attacker;
                var victim = @event.Userid;

                if (attacker == null || victim == null || !Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return HookResult.Continue;
                var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

                var victimPawn = victim!.PlayerPawn.Value;
                if (victimPawn == null || !victimPawn.IsValid) return HookResult.Continue;

                if (attackerInfo?.Skill == skillName)
                    if (playersToSlow.ContainsKey(victimPawn))
                        playersToSlow[victimPawn] = Server.TickCount + (64 * rubberTime);
                    else playersToSlow.TryAdd(victimPawn, Server.TickCount + (64 * rubberTime));

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                playersToSlow.Clear();
                return HookResult.Continue;
            });

            Instance.RegisterListener<Listeners.OnTick>(OnTick);
        }

        private static void OnTick()
        {
            foreach(var item in playersToSlow)
            {
                var pawn = item.Key;
                var time = item.Value;
                if (time >= Server.TickCount)
                    ChangeVelocity(pawn);
                else
                    playersToSlow.Remove(item.Key);
            }
        }

        private static void ChangeVelocity(CCSPlayerPawn pawn)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.VelocityModifier = rubberModifier;
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8B4513", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float slownessTime = 1.5f, float slownessModifier = .2f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float SlownessTime { get; set; } = slownessTime;
            public float SlownessModifier { get; set; } = slownessModifier;
        }
    }
}