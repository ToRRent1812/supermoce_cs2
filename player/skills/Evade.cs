using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Evade : ISkill
    {
        private const Skills skillName = Skills.Evade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                CCSPlayerController? victim = @event.Userid;
                if (victim == null || !Instance.IsPlayerValid(victim)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == victim.SteamID);
                if (playerInfo?.Skill == skillName && Instance.Random.NextDouble() <= 0.25)
                {
                    if (victim.PlayerPawn.Value == null || !victim.PawnIsAlive) return HookResult.Continue;

                    victim.PlayerPawn.Value.Health += @event.DmgHealth;
                    victim.PlayerPawn.Value.ArmorValue += @event.DmgArmor;

                    // Prevent the damage from being applied
                    @event.DmgHealth = 0;
                    @event.DmgArmor = 0;
                    return HookResult.Stop;
                }
                return HookResult.Continue;
            });
        }
        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#b2dd18", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
