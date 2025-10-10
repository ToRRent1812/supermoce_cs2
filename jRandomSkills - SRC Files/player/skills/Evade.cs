using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Evade : ISkill
    {
        private const Skills skillName = Skills.Evade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "CS2", "25% szans na uniknięcie trafienia", "#b2dd18");
        }

        public static void OnTakeDamage(DynamicHook h)
        {
            var param = h.GetParam<CEntityInstance>(0);
            var param2 = h.GetParam<CTakeDamageInfo>(1);

            if (param?.Entity == null || param2?.Attacker?.Value == null)
                return;

            var attackerPawn = new CCSPlayerPawn(param2.Attacker.Value.Handle);
            var victimPawn = new CCSPlayerPawn(param.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return;

            var attackerController = attackerPawn.Controller?.Value?.As<CCSPlayerController>();
            var victimController = victimPawn.Controller?.Value?.As<CCSPlayerController>();
            if (attackerController == null || victimController == null)
                return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == victimController.SteamID);
            if (playerInfo?.Skill == skillName && victimController.PawnIsAlive && Instance?.Random.Next(1, 5) == 1)
            {
                param2.Damage = 0f;
            }
        }
    }
}