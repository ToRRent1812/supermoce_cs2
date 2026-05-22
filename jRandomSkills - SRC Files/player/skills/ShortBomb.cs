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
            SkillUtils.RegisterSkill(skillName, "Pika paka", "Twoje C4 wybucha w 20 sekund.", "#4d4d4d", 1, 1);
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb != null)
                Server.NextFrame(() => {
                    if (plantedBomb != null && plantedBomb.IsValid)
                        plantedBomb.C4Blow = Server.CurrentTime + 20;
                });
        }
    }
}