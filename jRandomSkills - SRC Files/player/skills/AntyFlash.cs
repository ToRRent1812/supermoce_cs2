using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class AntyFlash : ISkill
    {
        private const Skills skillName = Skills.AntyFlash;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName, 
                "Super Flesz", 
                "Odporność na błysk. Twoje flesze działają dłużej", 
                "#D6E6FF");
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (attacker == null || !attacker.IsValid) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            var attackerInfo = SkillUtils.GetPlayerInfo(attacker);

            if (playerInfo?.Skill == skillName)
                playerPawn.FlashDuration = 0.0f;
            else if (attackerInfo?.Skill == skillName)
                playerPawn.FlashDuration = 6.5f;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Flashbang, 2);
        }
    }
}