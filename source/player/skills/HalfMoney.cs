using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class HalfMoney : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.HalfMoney;

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Skarbówka", 
            "Wybierasz gracza, któremu zabierzesz połowę kasy", 
            "#21e65c");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy))
                return;

            int moneyStolen = (enemy!.InGameMoneyServices?.Account ?? 0) / 2;
            AddMoney(enemy, -moneyStolen);
            AddMoney(player, moneyStolen);
            SkillUtils.PrintToChat(enemy, $" Wróg ukradł Ci połowę kasy {ChatColors.DarkRed}(-{moneyStolen}$)", true);
            SkillUtils.PrintToChat(player, $"Ukradłeś {moneyStolen}$.", false);
            if (playerInfo != null) playerInfo.SkillChance = 1;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            SkillUtils.DestroyTargetingSkill(player);
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