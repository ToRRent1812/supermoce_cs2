using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FriendlyFire : ISkill
    {
        private const Skills skillName = Skills.FriendlyFire;
        private static readonly string[] nades = ["inferno", "flashbang", "smokegrenade", "decoy", "hegrenade"];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Medyk", "Strzelanie do kolegów ich leczy", "#ff0000");
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var damage = @event.DmgHealth;
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            var weapon = @event.Weapon;
            HitGroup_t hitgroup = (HitGroup_t)@event.Hitgroup;

            if (nades.Contains(weapon)) return;
            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
            if (playerInfo?.Skill != skillName || attacker!.Team != victim!.Team) return;

            Server.ExecuteCommand("mp_autokick 0");

            var pawn = victim.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillUtils.AddHealth(pawn, damage + (int)(damage * 1.5f), pawn.MaxHealth);
        }
    }
}