using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class ShortBomb : ISkill
    {
        private const Skills skillName = Skills.ShortBomb;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Pika paka", 
            "Twoje C4 wybucha w 20 sekund.", 
            "#4d4d4d", 
            teamnum:1, 
            objective:1);
        }

        public static void BombPlanted(EventBombPlanted @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            Bomb.SetDetonationIn(20f);
        }
    }
}
