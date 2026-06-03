using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Planter : ISkill
    {
        private const Skills skillName = Skills.Planter;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Samowolka", 
            "Możesz podłożyć bombę w dowolnym miejscu (Trzymaj klawisz podkładania)", 
            "#7d7d7d", 
            teamnum:1,
            objective:1);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            Schema.SetSchemaValue(player!.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", false);
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!player.PawnIsAlive) continue;
                var playerInfo = SkillUtils.GetPlayerInfo(player);

                if (playerInfo?.Skill == skillName && Instance?.GameRules?.FreezePeriod == false)
                    Schema.SetSchemaValue(player.PlayerPawn.Value!.Handle, "CCSPlayerPawn", "m_bInBombZone", true);
            }
        }
    }
}