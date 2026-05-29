using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Armored : ISkill
    {
        private const Skills skillName = Skills.Armored;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Ulaniec", "Dostajesz % mniej obrażeń", "#d1430a");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            int randomValue = Instance?.Random?.Next(10,18) * 5 ?? 50; //50-85% dmg
            playerInfo.SkillChance = randomValue / 100f;
            playerInfo.RandomPercentage = (100-randomValue).ToString() + "% mniej";
            
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
            CCSPlayerController victim = victimPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker.SteamID);
            if (playerInfo == null) return HookResult.Continue;

            if (playerInfo.Skill == skillName && victim.PawnIsAlive)
            {
                float? skillChance = playerInfo.SkillChance;
                info.Damage *= skillChance ?? 1f;
            }
            return HookResult.Continue;
        }
    }
}