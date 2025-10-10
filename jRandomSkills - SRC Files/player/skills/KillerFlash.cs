using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class KillerFlash : ISkill
    {
        private const Skills skillName = Skills.KillerFlash;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Zabójczy Flesz", "Każdy oślepiony twoim granatem umiera (również ty i koledzy)", "#57bcff");
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (Instance?.IsPlayerValid(player) == false || Instance?.IsPlayerValid(attacker) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            var attackerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == attacker?.SteamID);

            if (attackerInfo?.Skill == skillName && playerInfo?.Skill != Skills.AntyFlash && player!.PlayerPawn.Value!.FlashDuration >= 0.5f)
                player?.PlayerPawn?.Value?.CommitSuicide(false, true);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Flashbang, 2);
        }
    }
}