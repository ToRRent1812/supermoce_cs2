using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ShortBomb : ISkill
    {
        private const Skills skillName = Skills.ShortBomb;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Pika Paka", "Twoja bomba wybucha znacznie szybciej", "#f5b74c", 2);
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb != null)
                Server.NextFrame(() => plantedBomb.C4Blow = (float)Server.EngineTime + 25f);
        }
    }
}