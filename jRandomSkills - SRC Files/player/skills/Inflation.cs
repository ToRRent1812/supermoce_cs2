using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Inflation : ISkill
    {
        private const Skills skillName = Skills.Inflation;
        private static int moneyLossPerBullet = 10;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Inflacja", "Każdy wystrzelony pocisk CT kosztuje ich pieniądze", "#4a944a", 1);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo != null)
            {
                playerInfo.SkillChance = Instance?.Random.Next(10, 41) ?? 10;
                moneyLossPerBullet += (int)(playerInfo.SkillChance ?? 0);
                playerInfo.RandomPercentage = $"{moneyLossPerBullet}$";
            }
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            moneyLossPerBullet = 0;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            playerInfo.SkillChance = 0;
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (player != null && playerInfo?.Skill != skillName && player.Team == CsTeam.CounterTerrorist)
                TakeMoney(player, moneyLossPerBullet);
        }

        private static void TakeMoney(CCSPlayerController player, int money)
        {
            if (player?.IsValid != true) return;
            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyServices.Account = Math.Max(moneyServices.Account - money, 0);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}