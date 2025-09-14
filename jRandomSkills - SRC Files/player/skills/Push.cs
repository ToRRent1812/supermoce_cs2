using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Push : ISkill
    {
        private const Skills skillName = Skills.Push;
        private static readonly float jumpVelocity = Config.GetValue<float>(skillName, "jumpVelocity");
        private static readonly float pushVelocity = Config.GetValue<float>(skillName, "pushVelocity");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"), false);
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;

            if (!Instance.IsPlayerValid(attacker) || !Instance.IsPlayerValid(victim) || attacker == victim)
                return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (playerInfo?.Skill == skillName && victim!.PawnIsAlive)
            {
                if (Instance.Random.NextDouble() <= playerInfo.SkillChance)
                    PushEnemy(victim, attacker!.PlayerPawn.Value!.EyeAngles);
            }
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
            //SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{Localization.GetTranslation("push")}{ChatColors.Lime}: " + Localization.GetTranslation("push_desc2", newChance), false);
        }

        private static void PushEnemy(CCSPlayerController player, QAngle attackerAngle)
        {
            if (player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.LifeState != (int)LifeState_t.LIFE_ALIVE)
                return;

            var currentPosition = player.PlayerPawn.Value.AbsOrigin;
            var currentAngles = player.PlayerPawn.Value.EyeAngles;

            Vector newVelocity = SkillUtils.GetForwardVector(attackerAngle) * pushVelocity;
            newVelocity.Z = player.PlayerPawn.Value.AbsVelocity.Z + jumpVelocity;

            player.PlayerPawn.Value.Teleport(currentPosition, currentAngles, newVelocity);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#1e9ab0", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float chanceFrom = .1f, float chanceTo = .25f, float jumpVelocity = 200f, float pushVelocity = 500f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float ChanceFrom { get; set; } = chanceFrom;
            public float ChanceTo { get; set; } = chanceTo;
            public float JumpVelocity { get; set; } = jumpVelocity;
            public float PushVelocity { get; set; } = pushVelocity;
        }
    }
}