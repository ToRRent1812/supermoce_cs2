using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Medic : ISkill
    {
        private const Skills skillName = Skills.Medic;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
            {
                Instance.AddTimer(0.1f, () =>
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        if (!Instance.IsPlayerValid(player)) continue;
                        player.RemoveItemByDesignerName("weapon_healthshot");
                        var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                        if (playerInfo?.Skill != skillName) continue;
                        EnableSkill(player);
                    }
                });

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!Instance.IsPlayerValid(player)) return HookResult.Continue;
                    DisableSkill(player);
                }

                return HookResult.Continue;
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid || !player.PawnIsAlive) return;
            int healthshot = Instance.Random.Next(3, 6);
            for (int i = 0; i < healthshot; i++)
                player.GiveNamedItem("weapon_healthshot");
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if(player == null || !player.IsValid || !player.PawnIsAlive) return;
            player.RemoveItemByDesignerName("weapon_healthshot");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#10c212", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}
