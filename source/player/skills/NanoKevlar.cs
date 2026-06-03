using CounterStrikeSharp.API;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class NanoKevlar : ISkill
    {
        private const Skills skillName = Skills.NanoKevlar;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "NanoKevlar", 
            "Pasywnie odnawiasz pancerz", 
            "#5bf4ff");
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
                int armorToAdd = Instance?.Random.Next(10, 21) ?? 10;
                pawn.ArmorValue = Math.Min(pawn.ArmorValue + armorToAdd, 100);
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }
        }
    }
}