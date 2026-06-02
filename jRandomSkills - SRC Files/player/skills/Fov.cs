using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Fov : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Fov;

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Ciepłe Piwo",
                "Możesz na kilka sekund zmienić pole widzenia losowego wroga",
                "#1466F5",
                minCooldown: 25,
                maxCooldown: 50,
                cooldownStep: 5);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
                ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config != null)
                ActiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            ActiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void UseSkill(CCSPlayerController player)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.CBodyComponent == null) return;
            if (!player.IsValid || !player.PawnIsAlive) return;

            if (!ActiveSkillFramework.CanUseSkill(skillName, player))
                return;

            CCSPlayerController[] enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0)
            {
                player.PrintToChat($" {ChatColors.Red}Nie znaleziono odpowiedniego wroga!");
                return;
            }

            CCSPlayerController randomEnemy = enemies.ElementAt(Instance?.Random.Next(0, enemies.Length) ?? 0);
            if (randomEnemy == null || !randomEnemy.IsValid || !randomEnemy.PawnIsAlive) return;

            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            ChangeFOV(randomEnemy);
        }

        private static void ChangeFOV(CCSPlayerController player)
        {
            if (player != null && player.IsValid && player.PawnIsAlive)
            {
                var randomfov = Instance?.Random.Next(1, 4) ?? 1;

                switch (randomfov)
                {
                    case 1:
                        player.DesiredFOV = 15;
                        break;
                    case 2:
                        player.DesiredFOV = 23;
                        break;
                    case 3:
                        player.DesiredFOV = 150;
                        break;
                    default:
                        player.DesiredFOV = 55;
                        break;
                }
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                SkillUtils.PrintToChat(player, $" Przeciwnik Cię upił! Zaraz wytrzeźwiejesz");
                Instance?.AddTimer(9.0f, () =>
                {
                    player.DesiredFOV = 0;
                    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                });
            }
        }
    }
}