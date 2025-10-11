using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Planter : ISkill
    {
        private const Skills skillName = Skills.Planter;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Samowolka", "Możesz podłożyć bombę w dowolnym miejscu (Trzymaj klawisz podkładania)", "#7d7d7d", 1);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            Schema.SetSchemaValue(player!.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", false);
        }

        public static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName && Instance?.GameRules?.FreezePeriod == false)
                    Schema.SetSchemaValue(player!.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", true);
            }
        }
    }
}