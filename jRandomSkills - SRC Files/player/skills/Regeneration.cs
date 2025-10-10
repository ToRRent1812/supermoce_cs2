using CounterStrikeSharp.API;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Regeneration : ISkill
    {
        private const Skills skillName = Skills.Regeneration;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Regeneracja", "Pasywnie odnawiasz zdrowie", "#fff12e");
        }

        public static void OnTick()
        {
            if (Server.TickCount % 32 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;
                SkillUtils.AddHealth(pawn, Instance?.Random.Next(3,9) ?? 0);
            }
        }
    }
}