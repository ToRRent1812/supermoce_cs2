using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Planter : ISkill
    {
        private const Skills skillName = Skills.Planter;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (!Instance.IsPlayerValid(player)) return;
            Schema.SetSchemaValue<bool>(player!.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", false);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Instance.IsPlayerValid(player)) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName && Instance?.GameRules?.FreezePeriod == false)
                    Schema.SetSchemaValue<bool>(player!.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", true);
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#7d7d7d", CsTeam onlyTeam = CsTeam.Terrorist, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}