using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Rapper : ISkill
    {
        private const Skills skillName = Skills.Rapper;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "50 Cent", "Zarabiasz dla siebie, i drużyny 50$ za każdy wystrzelony pocisk", "#3de61c");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo != null)
                playerInfo.SkillChance = 0;
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName && playerInfo.SkillChance > 0)
                    UpdateHUD(player, playerInfo.SkillChance);
            }
        }

        private static void UpdateHUD(CCSPlayerController player, float? moneybonus = 0)
        {
            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null || player?.IsValid != true || !player.PawnIsAlive || Instance?.GameRules == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name} (+{moneybonus}$)</font> <br>";
            string remainingLine = $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillData.Description}</font> ";

            player.PrintToCenterHtml(skillLine + remainingLine);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            AddMoney(player, (int)(playerInfo.SkillChance ?? 0));
            var teammates = Utilities.GetPlayers().Where(p =>
                p.Team == player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist).ToArray();
            foreach (var teammate in teammates)
                AddMoney(teammate, (int)(playerInfo.SkillChance ?? 0));
            playerInfo.SkillChance = 0;
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
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