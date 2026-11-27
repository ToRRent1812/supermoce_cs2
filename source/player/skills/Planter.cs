using CounterStrikeSharp.API.Core;
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
            "Możesz podłożyć bombę w dowolnym miejscu", 
            "#7d7d7d", 
            teamnum:1,
            objective:1);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            Bomb.SetInBombZone(player!.PlayerPawn.Value, false);
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!player.PawnIsAlive) continue;
                var playerInfo = SkillUtils.GetPlayerInfo(player);

                if (playerInfo?.Skill == skillName && Instance?.GameRules?.FreezePeriod == false)
                    Bomb.SetInBombZone(player.PlayerPawn.Value, true);
            }
        }
    }
}