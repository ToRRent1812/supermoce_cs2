using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;

namespace Supermoce
{
    public class Rapper : ISkill
    {
        private const Skills skillName = Skills.Rapper;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "50 Cent", 
            "Zarabiasz dla siebie, i drużyny 50$ za każdy wystrzelony pocisk", 
            "#3de61c");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo != null)
                playerInfo.SkillChance = 0;
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName && playerInfo.SkillChance > 0)
                    UpdateHUD(player, playerInfo.SkillChance);
            }
        }

        private static void UpdateHUD(CCSPlayerController player, float? moneybonus = 0)
        {
            if (player?.IsValid != true || !player.PawnIsAlive) return;
            SkillHudPanel.ShowStatus(player, skillName, $"Bonus: +{moneybonus}$");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;
            var teammates = SkillUtils.CachedPlayers.Where(p =>
                p.Team == player.Team && p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist).ToArray();
            foreach (var teammate in teammates)
                AddMoney(teammate, (int)(playerInfo.SkillChance ?? 0));
            playerInfo.SkillChance = 0;
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
                playerInfo.SkillChance += 50;
        }

        private static void AddMoney(CCSPlayerController player, int money)
        {
            if (player?.IsValid != true) return;
            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null) return;

            moneyServices.Account = Math.Max(moneyServices.Account + money, 0);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}