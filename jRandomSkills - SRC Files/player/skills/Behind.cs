using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Behind : ISkill
    {
        private const Skills skillName = Skills.Behind;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event!.Attacker;
            var victim = @event!.Userid;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim) return;
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
                if (Instance.Random.NextDouble() <= playerInfo.SkillChance)
                    RotateEnemy(victim);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;
            float newChance = (float)Instance.Random.NextDouble() * (Config.GetValue<float>(skillName, "ChanceTo") - Config.GetValue<float>(skillName, "ChanceFrom")) + Config.GetValue<float>(skillName, "ChanceFrom");
            playerInfo.SkillChance = newChance;
            newChance = (float)Math.Round(newChance, 2) * 100;
            newChance = (float)Math.Round(newChance);
            playerInfo.RandomPercentage = newChance.ToString() + "%";
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("behind")}{ChatColors.Lime}: " + Localization.GetTranslation("behind_desc2", newChance), false);
        }

        private static void RotateEnemy(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;

            if (pawn == null || !pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

            var currentPosition = pawn.AbsOrigin;
            var currentAngles = pawn.EyeAngles;

            QAngle newAngles = new(
                currentAngles.X,
                currentAngles.Y + 180,
                currentAngles.Z
            );

            pawn.Teleport(currentPosition, newAngles, new Vector(0, 0, 0));
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#00FF00", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float chanceFrom = .1f, float chanceTo = .25f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
        }
    }
}