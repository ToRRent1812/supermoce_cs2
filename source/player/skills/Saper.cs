using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Saper : ISkill
    {
        private const Skills skillName = Skills.Saper;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Saper", 
            "Natychmiastowy plant i rozbrojenie z twoich rąk", 
            "#8A2BE2", 
            objective:1);
        }

        public static void BombBegindefuse(EventBombBegindefuse @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == true)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName)
                {
                    Bomb.ForceDefuse();
                }
            }
        }

        public static void BombBeginplant(EventBombBeginplant @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
            {
                Bomb.ArmInstantly();
            }
        }
    }
}