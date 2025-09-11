using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ShortBomb : ISkill
    {
        private const Skills skillName = Skills.ShortBomb;
        private static readonly int detonationTime = Config.GetValue<int>(skillName, "detonationTime");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventBombPlanted>((@event, info) =>
            {
                var player = @event.Userid;
                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
                if (playerInfo?.Skill != skillName) return HookResult.Continue;

                var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                if (plantedBomb != null)
                {
                    Server.NextFrame(() => plantedBomb.C4Blow = (float)Server.EngineTime + detonationTime);
                }

                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#f5b74c", CsTeam onlyTeam = CsTeam.Terrorist, bool needsTeammates = false, int detonationTime = 20) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int DetonationTime { get; set; } = detonationTime;
        }
    }
}
