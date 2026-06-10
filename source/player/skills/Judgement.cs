using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Judgement : ISkill
    {
        private const Skills skillName = Skills.Judgement;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Granat sprawiedliwości", 
            "Każdy oślepiony wróg z twojej ręki traci połowę życia", 
            "#ff5757");
        }

        public static void PlayerBlind(EventPlayerBlind @event)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (player == null || attacker == null) return;
            if (Instance?.IsPlayerValid(player) == false || Instance?.IsPlayerValid(attacker) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            var attackerInfo = SkillUtils.GetPlayerInfo(attacker);
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            if (attackerInfo?.Skill == skillName && playerInfo?.Skill != Skills.AntyFlash && pawn.FlashDuration >= 1.0f && player.Team != attacker.Team)
                SkillUtils.TakeHealth(pawn, pawn.Health / 2);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            SkillUtils.TryGiveWeapon(player, CsItem.Flashbang, 2);
        }
    }
}