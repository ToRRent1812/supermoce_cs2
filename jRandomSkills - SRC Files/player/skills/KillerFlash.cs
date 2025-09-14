using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class KillerFlash : ISkill
    {
        private const Skills skillName = Skills.KillerFlash;
        private static readonly float flashDuration = Config.GetValue<float>(skillName, "flashDuration");

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (!Instance.IsPlayerValid(player) || !Instance.IsPlayerValid(attacker)) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            var attackerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && playerInfo?.Skill != Skills.AntyFlash && player!.PlayerPawn.Value!.FlashDuration >= flashDuration)
                player?.PlayerPawn?.Value?.CommitSuicide(false, true);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Flashbang, 2);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#57bcff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, float flashDuration = 0.5f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float FlashDuration { get; set; } = flashDuration;
        }
    }
}