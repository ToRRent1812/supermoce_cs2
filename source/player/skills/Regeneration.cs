using CounterStrikeSharp.API;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Regeneration : ISkill
    {
        private const Skills skillName = Skills.Regeneration;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Regeneracja", 
            "Pasywnie odnawiasz zdrowie", 
            "#fff12e");
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill != skillName) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;
                SkillUtils.AddHealth(pawn, Instance?.Random.Next(3,7) ?? 3);
            }
        }
    }
}