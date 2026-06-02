using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RichBoy : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.RichBoy;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Bogacz",
                "Na początku rundy otrzymujesz wypłatę. Zabicie wroga też przyznaje bonus.",
                "#D4AF37",
                minValue: 1,
                maxValue: 10,
                step: 1,
                customValueFormatter: value => $"+{value}000$");

            Server.ExecuteCommand("mp_maxmoney 99999");
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
                int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                int moneyBonus = randomValue * 1000;
                playerInfo.SkillChance = moneyBonus;
                AddMoney(player, moneyBonus);
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 0;
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            CCSPlayerController? victimPlayer = @event.Userid;
            CCSPlayerController? attackerPlayer = @event.Attacker;

            if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid || attackerPlayer.TeamNum == victimPlayer.TeamNum) return;
            if (attackerPlayer == victimPlayer) return;

            var attackerInfo = SkillUtils.GetPlayerInfo(attackerPlayer);

            if (attackerInfo?.Skill == skillName)
            {
                var moneyServices = attackerPlayer?.InGameMoneyServices;
                if (moneyServices == null) return;

                AddMoney(attackerPlayer!, (int)(attackerInfo.SkillChance ?? 0));
            }
        }

        private static void AddMoney(CCSPlayerController player, int money)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyServices.Account = Math.Max(moneyServices.Account + money, 0);
            Utilities.SetStateChanged(player!, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}