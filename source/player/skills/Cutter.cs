using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Cutter : ISkill
    {
        private const Skills skillName = Skills.Cutter;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Maczeta", 
            "Natychmiastowe zabójstwo nożem", 
            "#88a31a", 
            teamnum:1);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var weapon = @event.Weapon;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo?.Skill != skillName) return;

            if (weapon == "knife")
                SkillUtils.TakeHealth(victim!.PlayerPawn.Value, 999);
        }
    }
}