using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Watchmaker : ISkill
    {
        private const Skills skillName = Skills.Watchmaker;
        private static bool bombPlanted = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Sędzia Kalosz", 
            "Rzut granatem zmienia po cichu czas rundy", 
            "#ff462e", 
            objective:1);
        }

        public static void NewRound()
        {
            bombPlanted = false;
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            bombPlanted = true;
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (bombPlanted) return;
            var name = entity.DesignerName;
            if (!name.EndsWith("_projectile"))
                return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;

            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;
            var player = pawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName || Instance?.GameRules == null) return;

            if (Instance != null && Instance.GameRules != null)
            {
                if(player.Team == CsTeam.CounterTerrorist && Instance.GameRules.RoundTime > 10) Instance.GameRules.RoundTime -= 10;
                else if(player.Team == CsTeam.Terrorist) Instance.GameRules.RoundTime += 10;
            }
        }

        public static void OnTick()
        {
            if (bombPlanted) return;
            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill == skillName)
                    UpdateHUD(player);
            }
        }

        private static void UpdateHUD(CCSPlayerController player)
        {
            var rules = Instance?.GameRules;
            if (rules == null) return;

            int seconds = 1 + (int)(rules.RoundTime - (Server.CurrentTime - rules.RoundStartTime));
            SkillHudPanel.ShowStatus(player, skillName, $"Prawdziwy czas rundy: {SkillUtils.SecondsToTimer(seconds)}");
        }
    }
}