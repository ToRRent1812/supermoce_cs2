using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Thorns : ISkill
    {
        private const Skills skillName = Skills.Thorns;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Lustro", "Przeciwnik obrywa 15% obrażeń, które Ci zadaje", "#962631");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var victimInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victim?.SteamID);
            if (victimInfo?.Skill == skillName && victim!.PawnIsAlive && attacker!.PawnIsAlive)
            {
                SkillUtils.TakeHealth(attacker.PlayerPawn.Value, (int)(@event.DmgHealth * .15f));
                attacker.EmitSound("Player.DamageBody.Onlooker", volume: 0.2f);
            }
        }
    }
}