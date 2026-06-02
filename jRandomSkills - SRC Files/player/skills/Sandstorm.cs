using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;
using System.Drawing;

namespace jRandomSkills
{
    public class Sandstorm : ISkill, IActiveSkill
    {
        private const Skills skillName = Skills.Sandstorm;
        private static readonly ConcurrentDictionary<ulong, byte> activeFogPlayers = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterActiveSkill(
                skillName,
                "Burza piaskowa",
                "Możesz na 10 sekund nałożyć efekt burzy piaskowej dla losowego wroga",
                "#8B4513",
                minCooldown: 25,
                maxCooldown: 60,
                cooldownStep: 5);
        }

        public static void NewRound()
        {
            CleanupAllFog();
            activeFogPlayers.Clear();
            ActiveSkillFramework.OnNewRound();
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
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;

            var config = SkillUtils.GetActiveSkillConfig(skillName);
            if (config == null) return;
            if (!ActiveSkillFramework.CanUseSkill(skillName, player)) return;

            var enemies = SkillUtils.GetAliveEnemies(player);
            if (enemies.Length == 0) return;

            var enemy = enemies[Instance?.Random.Next(enemies.Length) ?? 0];

            ApplyFogToEnemy(enemy);
            ActiveSkillFramework.MarkSkillUsed(skillName, player);
            SkillUtils.PrintToChat(enemy, $" Wróg wyczarował tobie burzę piaskową!", true);
        }

        private static void ApplyFogToEnemy(CCSPlayerController enemy)
        {
            if (enemy == null || !enemy.IsValid || enemy.Pawn?.Value == null) return;

            RemoveFogFromEnemy(enemy);

            var fogController = GetOrCreateFogController(enemy);
            if (fogController == null) return;

            fogController.Fog.Enable = true;
            Color color = ColorTranslator.FromHtml("#8B4513");
            fogController.Fog.ColorPrimary = color;
            fogController.Fog.Exponent = 1.0f;
            fogController.Fog.Maxdensity = 0.85f;
            fogController.Fog.End = 200f;

            ChangePlayerVisibility(0.3f);

            enemy.Pawn.Value.AcceptInput("SetFogController", fogController, fogController, "!activator");

            activeFogPlayers.TryAdd(enemy.SteamID, 0);

            Instance?.AddTimer(10f, () =>
            {
                RemoveFogFromEnemy(enemy);
            });
        }

        private static void RemoveFogFromEnemy(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            activeFogPlayers.TryRemove(player.SteamID, out _);

            CFogController? fogController = GetOrCreateFogController(player);
            if (fogController == null) return;
            fogController.Remove();
        }

        private static void CleanupAllFog()
        {
            foreach (var steamID in activeFogPlayers.Keys)
            {
                var player = Utilities.GetPlayerFromSteamId(steamID);
                if (player != null && player.IsValid)
                    RemoveFogFromEnemy(player);
            }
        }

        private static CFogController? GetOrCreateFogController(CCSPlayerController player)
        {
            string fogControllerName = $"SandstormFog_{player.Slot}";

            foreach (CFogController? entry in Utilities.FindAllEntitiesByDesignerName<CFogController>("env_fog_controller"))
            {
                if (entry.Entity!.Name == fogControllerName)
                {
                    return entry;
                }
            }

            CFogController? envFogController = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
            if (envFogController == null) return null;

            envFogController.Entity!.Name = fogControllerName;
            envFogController.DispatchSpawn();
            return envFogController;
        }

        private static void ChangePlayerVisibility(float visibility = 0.9f)
        {
            CPlayerVisibility? envPlayerVisibility = Utilities.FindAllEntitiesByDesignerName<CPlayerVisibility>("env_player_visibility").FirstOrDefault();
            if (envPlayerVisibility == null)
            {
                envPlayerVisibility = Utilities.CreateEntityByName<CPlayerVisibility>("env_player_visibility");
                if (envPlayerVisibility == null) return;
                envPlayerVisibility.DispatchSpawn();
            }

            envPlayerVisibility.FogMaxDensityMultiplier = visibility;
            Utilities.SetStateChanged(envPlayerVisibility, "CPlayerVisibility", "m_flFogMaxDensityMultiplier");
        }
    }
}