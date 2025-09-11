using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Thorns : ISkill
    {
        private const Skills skillName = Skills.Thorns;
        private static readonly float healthTakenScale = Config.GetValue<float>(skillName, "healthTakenScale");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            
            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                var attacker = @event.Attacker;
                var victim = @event.Userid;

                if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return HookResult.Continue;
                var victimInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
                if (victimInfo?.Skill == skillName && victim!.PawnIsAlive && attacker!.PawnIsAlive)
                {
                    SkillUtils.TakeHealth(attacker.PlayerPawn.Value, (int)(@event.DmgHealth * healthTakenScale));
                    attacker.EmitSound("Player.DamageBody.Onlooker", volume: 0.3f);
                }
                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#962631", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float healthTakenScale = .15f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float HealthTakenScale { get; set; } = healthTakenScale;
        }
    }
}