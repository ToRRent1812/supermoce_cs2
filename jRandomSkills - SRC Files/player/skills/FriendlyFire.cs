using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FriendlyFire : ISkill
    {
        private const Skills skillName = Skills.FriendlyFire;
        private static readonly float healthMultiplier = Config.GetValue<float>(skillName, "healthMultiplier");
        private static readonly string[] nades = ["inferno", "flashbang", "smokegrenade", "decoy", "hegrenade"];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var damage = @event.DmgHealth;
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            var weapon = @event.Weapon;
            HitGroup_t hitgroup = (HitGroup_t)@event.Hitgroup;

            if (nades.Contains(weapon)) return;
            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
            if (playerInfo?.Skill != skillName || attacker!.Team != victim!.Team) return;

            Server.ExecuteCommand("mp_autokick 0");

            var pawn = victim.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillUtils.AddHealth(pawn, damage + (int)(damage * healthMultiplier), pawn.MaxHealth);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ff0000", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = true, float healthMultiplier = 1.5f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float HealthMultiplier { get; set; } = healthMultiplier;
        }
    }
}