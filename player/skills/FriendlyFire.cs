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

            Instance.RegisterEventHandler<EventPlayerHurt>((@event, info) =>
            {
                var damage = @event.DmgHealth;
                var victim = @event.Userid;
                var attacker = @event.Attacker;
                var weapon = @event.Weapon;
                HitGroup_t hitgroup = (HitGroup_t)@event.Hitgroup;

                if (nades.Contains(weapon)) return HookResult.Continue;
                if (attacker == null || victim == null || !Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);
                if (playerInfo?.Skill != skillName || attacker!.Team != victim!.Team) return HookResult.Continue;

                Server.ExecuteCommand("mp_autokick 0");

                var pawn = victim.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) return HookResult.Continue;
                SkillUtils.AddHealth(pawn, damage + (int)(damage * healthMultiplier), pawn.MaxHealth);
                
                return HookResult.Continue;
            });
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ff0000", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = true, float healthMultiplier = 1.5f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float HealthMultiplier { get; set; } = healthMultiplier;
        }
    }
}