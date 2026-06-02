using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using jRandomSkills.src.player;

namespace jRandomSkills
{
    public class BrokenKnee : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.BrokenKnee;

        private static readonly ConcurrentDictionary<ulong, int> affectedPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(
                skillName, 
                "Strzał w kolano", 
                "Wybierasz gracza, który będzie chodzić wolniej", 
                "#e68a21");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
            affectedPlayers.Clear();
            foreach (var player in SkillUtils.CachedPlayers)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null && player.PawnIsAlive)
                    playerPawn.VelocityModifier = 1f;
            }
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!affectedPlayers.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || !player.PawnIsAlive || playerPawn.VelocityModifier == 0.75f) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 0.75f;
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy))
                return;
            
            if(enemy == null || !enemy.IsValid || !enemy.PawnIsAlive) return;
            SkillUtils.PrintToChat(enemy, $" Wróg spowodował że poruszasz się wolniej");
            affectedPlayers.TryAdd(enemy.SteamID, 0);
            if (playerInfo != null) playerInfo.SkillChance = 1;
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillEnabled(skillName, player);
            SkillUtils.InitTargetingSkill(player, skillName);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            MenuSkillFramework.OnSkillDisabled(player);
            SkillUtils.DestroyTargetingSkill(player);
        }
    }
}