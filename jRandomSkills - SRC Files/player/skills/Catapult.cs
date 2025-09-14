using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Catapult : ISkill
    {
        private const Skills skillName = Skills.Catapult;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && victim!.PawnIsAlive)
                if (Instance.Random.NextDouble() <= attackerInfo.SkillChance)
                {
                    var victimPawn = victim.PlayerPawn?.Value;
                    if (victimPawn != null)
                        victimPawn.AbsVelocity.Z = 300f;
                }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            float newChance = (float)Instance.Random.NextDouble() * (Config.GetValue<float>(skillName, "chanceTo") - Config.GetValue<float>(skillName, "chanceFrom")) + Config.GetValue<float>(skillName, "chanceFrom");
            playerInfo.SkillChance = newChance;
            newChance = (float)Math.Round(newChance, 2) * 100;
            newChance = (float)Math.Round(newChance);
            playerInfo.RandomPercentage = newChance.ToString() + "%";
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("catapult")}{ChatColors.Lime}: " + Localization.GetTranslation("catapult_desc2", newChance), false);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#FF4500", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float chanceFrom = .1f, float chanceTo = .25f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
        }
    }
}