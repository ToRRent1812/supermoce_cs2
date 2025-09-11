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
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
            
            Instance.RegisterEventHandler<EventBombBeginplant>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
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

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventBombBegindefuse>((@event, info) =>
            {
                var player = @event.Userid;
                
                if (player != null && Instance.IsPlayerValid(player))
                {
                    var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                    if (playerInfo?.Skill == skillName)
                    {
                        var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                        if (plantedBomb != null)
                        {
                            Server.NextFrame(() =>
                            {
                                plantedBomb.DefuseCountDown = 0;
                            });
                        }
                    }
                }

                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#8A2BE2", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}