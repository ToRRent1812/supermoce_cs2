using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class NoNades : ISkill
    {
        private const Skills skillName = Skills.NoNades;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Super Armor", 
            "Granaty nie zadają Ci obrażeń", 
            "#a38c1a");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var damage = @event.DmgHealth;
            var player = @event.Userid;
            var weapon = @event.Weapon;

            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            if (weapon == "hegrenade" || weapon == "inferno")
            {
                SkillUtils.AddHealth(player!.PlayerPawn.Value, damage);
                damage = 0;
            }
        }
    }
}