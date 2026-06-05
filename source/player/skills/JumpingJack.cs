using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class JumpingJack : ISkill
    {
        private const Skills skillName = Skills.JumpingJack;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Skakanka", 
            "Skakanie przywraca zdrowie", 
            "#a86eff");
        }

        public static void PlayerJump(EventPlayerJump @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;
            SkillUtils.AddHealth(player.PlayerPawn.Value, Instance?.Random.Next(8, 16) ?? 8);
        }
    }
}