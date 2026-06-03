using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Disarmament : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.Disarmament;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Rozbrojenie",
                "Szansa na wyrzucenie broni wroga po trafieniu",
                "#FF4500",
                minValue: 10,
                maxValue: 30,
                step: 1,
                customValueFormatter: (value) => $"{value}% szans");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);

                int randomRoll = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                playerInfo.SkillChance = randomRoll / 100f;
            }
        }

        public static void NewRound()
        {
            foreach (var player in SkillUtils.CachedPlayers)
                DisableSkill(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 1f;
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false || attacker == victim) return;
            var playerInfo = SkillUtils.GetPlayerInfo(attacker);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                if (Instance?.Random.NextDouble() <= playerInfo?.SkillChance)
                {
                    var weaponServices = victim.PlayerPawn?.Value?.WeaponServices;
                    if (weaponServices?.ActiveWeapon == null) return;

                    var weaponName = weaponServices?.ActiveWeapon?.Value?.DesignerName;
                    if (weaponName != null && !weaponName.Contains("weapon_knife") && !weaponName.Contains("weapon_c4"))
                        victim.DropActiveWeapon();
                }
            }
        }
    }
}