using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Cutter : ISkill
    {
        private const Skills skillName = Skills.Cutter;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Maczeta", "Natychmiastowe zabójstwo nożem", "#88a31a", 1);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var damage = @event.DmgHealth;
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var weapon = @event.Weapon;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
            if (playerInfo?.Skill != skillName) return;

            if (weapon == "knife")
                SkillUtils.TakeHealth(victim!.PlayerPawn.Value, 999);
        }
    }
}