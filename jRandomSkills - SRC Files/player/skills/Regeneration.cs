using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Regeneration : ISkill
    {
        private const Skills skillName = Skills.Regeneration;
        private static float cooldown = Config.GetValue<float>(skillName, "cooldown");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void OnTick()
        {
            if (Server.TickCount % (int)(64 * cooldown) != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill != skillName) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;
                SkillUtils.AddHealth(pawn, Instance.Random.Next(3,9));
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#fff12e", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float Cooldown { get; set; } = 1f;
        }
    }
}