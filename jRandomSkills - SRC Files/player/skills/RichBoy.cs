using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RichBoy : ISkill
    {
        private const Skills skillName = Skills.RichBoy;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Bogacz", "Na początku rundy otrzymujesz wypłatę. Zabicie wroga podwaja twoje konto", "#D4AF37");
            Server.ExecuteCommand("mp_maxmoney 99999");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            int moneyBonus = (Instance?.Random.Next(5000, 10000)) ?? 5000;
            playerInfo.SkillChance = moneyBonus;
            AddMoney(player, moneyBonus);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            AddMoney(player, -(int)(playerInfo.SkillChance ?? 0));
            playerInfo.SkillChance = 0;
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            CCSPlayerController? victimPlayer = @event.Userid;
            CCSPlayerController? attackerPlayer = @event.Attacker;

            if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid || attackerPlayer.TeamNum == victimPlayer.TeamNum) return;
            if (attackerPlayer == victimPlayer) return;

            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);

            if (attackerInfo?.Skill == skillName)
            {
                var moneyServices = attackerPlayer?.InGameMoneyServices;
                if (moneyServices == null) return;

                moneyServices.Account *= 2;
                Utilities.SetStateChanged(attackerPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
            }
        }

        private static void AddMoney(CCSPlayerController player, int money)
        {
            if (player == null || !player.IsValid) return;
            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyServices.Account = Math.Max(moneyServices.Account + money, 0);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}