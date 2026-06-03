using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class ChaoticDamage : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.ChaoticDamage;
        private static bool Randomness = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Podejrzane naboje",
                "Możesz na 10 sek. włączyć losowe obrażenia na serwerze",
                "#a8720c",
                minCooldown: 20,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            Randomness = false;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            if (!player.IsValid || Instance?.GameRules?.FreezePeriod == true) return;
            var playerPawn = player.PlayerPawn?.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);

            Randomness = true;
            SkillUtils.PrintToChatAll($"UWAGA! {ChatColors.LightRed}włączono losowy damage na 10 sek!", false);

            Instance?.AddTimer(10.0f, () => Randomness = false);
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (!Randomness) return HookResult.Continue;

            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn == null || attackerPawn.Controller?.Value == null || victimPawn == null || victimPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null) return HookResult.Continue;

            var activeWeapon = attackerPawn.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon != null && attacker.PawnIsAlive)
            {
                info.Damage = Instance?.Random.Next(1, 99) ?? 1;
            }

            return HookResult.Continue;
        }
    }
}