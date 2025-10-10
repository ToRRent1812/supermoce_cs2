using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Saper : ISkill
    {
        private const Skills skillName = Skills.Saper;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Saper", "Niemal natychmiastowy plant i rozbrojenie z twoich rąk", "#8A2BE2", 3);
        }

        public static void BombBegindefuse(EventBombBegindefuse @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == true)
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
                if (playerInfo?.Skill == skillName)
                {
                    var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                    if (plantedBomb != null)
                        Server.NextFrame(() => plantedBomb.DefuseCountDown = 0);
                }
            }
        }

        public static void BombBeginplant(EventBombBeginplant @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill == skillName)
            {
                var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList();
                if (bombEntities.Count != 0)
                {
                    var bomb = bombEntities.FirstOrDefault();
                    if (bomb != null)
                    {
                        bomb.BombPlacedAnimation = false;
                        bomb.ArmedTime = 0.0f;
                    }
                }
            }
        }
    }
}