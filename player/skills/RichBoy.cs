using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class RichBoy : ISkill
    {
        private const Skills skillName = Skills.RichBoy;
        private static readonly int minMoney = Config.GetValue<int>(skillName, "minMoney");
        private static readonly int maxMoney = Config.GetValue<int>(skillName, "maxMoney");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                        if (playerInfo?.Skill == skillName)
                        {
                            int moneyBonus = Instance.Random.Next(minMoney, maxMoney);
                            playerInfo.SkillChance = moneyBonus;
                            AddMoney(player, moneyBonus);
                        }
                    }
                });
                return HookResult.Continue;
            });
            
            Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
            {
                CCSPlayerController? victimPlayer = @event.Userid;
                CCSPlayerController? attackerPlayer = @event.Attacker;

                if (attackerPlayer == victimPlayer) return HookResult.Continue;
                if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid) return HookResult.Continue;

                var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attackerPlayer.SteamID);

                if (attackerInfo?.Skill == skillName)
                {
                    var moneyServices = attackerPlayer?.InGameMoneyServices;
                    if (moneyServices == null) return HookResult.Continue;

                    moneyServices.Account *= 2;
                    if (attackerPlayer != null)
                    {
                        Utilities.SetStateChanged(attackerPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
                    }
                }
                    
                return HookResult.Continue;
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            int moneyBonus = Instance.Random.Next(minMoney, maxMoney);
            playerInfo.SkillChance = moneyBonus;
            AddMoney(player, moneyBonus);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            AddMoney(player, -(int)(playerInfo.SkillChance ?? 0));
        }

        private static void AddMoney(CCSPlayerController player, int money)
        {
            if (player == null || !player.IsValid) return;
            var moneyServices = player.InGameMoneyServices;

            if (moneyServices == null) return;

            moneyServices.Account = Math.Max(moneyServices.Account + money, 0);
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#D4AF37", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int minMoney = 5000, int maxMoney = 10000) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int MinMoney { get; set; } = minMoney;
            public int MaxMoney { get; set; } = maxMoney;
        }
    }
}