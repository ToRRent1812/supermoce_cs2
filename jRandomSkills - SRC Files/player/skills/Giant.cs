using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Giant : ISkill, IMenuSkill
    {
        private const Skills skillName = Skills.Giant;
        private static readonly ConcurrentDictionary<ulong, int> giants = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterMenuSkill(skillName, 
            "Olbrzym", 
            "Wybierasz wroga, którego powiększysz", 
            "#e621d6");
        }

        public static void NewRound()
        {
            MenuSkillFramework.OnNewRound();
            giants.Clear();
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (Instance?.IsPlayerValid(player) == false) continue;
                SkillUtils.ResetPlayerMovement(player);
                SkillUtils.ChangePlayerScale(player, 1f);
            }
        }

        public static void OnTick()
        {
            foreach (var player in SkillUtils.CachedPlayers)
            {
                if (!giants.ContainsKey(player.SteamID)) continue;

                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || playerPawn.VelocityModifier != 0.7f) continue;

                var buttons = player.Buttons;
                if (buttons.HasFlag(PlayerButtons.Moveleft) || buttons.HasFlag(PlayerButtons.Moveright) || buttons.HasFlag(PlayerButtons.Forward) || buttons.HasFlag(PlayerButtons.Back))
                    playerPawn.VelocityModifier = 0.7f;
            }
        }

        public static void TypeSkill(CCSPlayerController player, string[] commands)
        {
            if (!SkillUtils.TryGetTargetFromCommand(player, skillName, commands, out var playerInfo, out var enemy))
                return;

            SkillUtils.ChangePlayerScale(enemy!, 2f);
            var enemyPawn = enemy!.PlayerPawn.Value;
            if (enemyPawn != null && enemyPawn.IsValid)
                enemyPawn.ActualGravityScale = 0.9f;
            SkillUtils.PrintToChat(enemy, $" Wróg Cię powiększył");
            giants.TryAdd(enemy.SteamID, 0);
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
            SkillUtils.DestroyTargetingSkill(player, p => giants.TryRemove(p.SteamID, out _));
        }
    }
}