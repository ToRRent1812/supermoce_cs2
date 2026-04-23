using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Impostor : ISkill
    {
        private const Skills skillName = Skills.Impostor;
        private static readonly string defaultCTModel = "agents/models/ctm_sas.vmdl";
        private static readonly string defaultTModel = "agents/models/tm_phoenix_heavy.vmdl";
        private static readonly ConcurrentDictionary<ulong, string> originalModels = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Szpieg", "Wyglądasz jak przeciwnik", "#99140B");
        }

        public static void NewRound()
        {
            originalModels.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            string model = GetEnemyModel(player);
            if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.CBodyComponent != null && player.PlayerPawn.Value.CBodyComponent.SceneNode != null)
                originalModels.TryAdd(player.SteamID, player.PlayerPawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName);
            PrecacheModel(model);
            SetPlayerModel(player, model);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var model = originalModels.TryGetValue(player.SteamID, out var originalModel) && !string.IsNullOrEmpty(originalModel) ? originalModel :
                player.Team == CsTeam.Terrorist ? defaultTModel : defaultCTModel;
            PrecacheModel(model);
            SetPlayerModel(player, model);
            originalModels.TryRemove(player.SteamID, out _);
        }

        private static void PrecacheModel(string model)
        {
            if (string.IsNullOrEmpty(model)) return;
            var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (prop == null || !prop.IsValid) return;

            try
            {
                prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
                prop.SetModel(model);
                prop.Render = Color.FromArgb(0, 255, 255, 255);
                prop.Teleport(new Vector(0, 0, 0), new QAngle(0, 0, 0));
                prop.DispatchSpawn();
                prop.AcceptInput("InitializeSpawnFromWorld", prop, prop, "");
                Utilities.SetStateChanged(prop, "CBaseEntity", "m_CBodyComponent");
                if (prop.CBodyComponent != null && prop.CBodyComponent.SceneNode != null)
                    prop.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = 1;
                Server.NextFrame(() => prop.AcceptInput("SetScale", prop, prop, "1"));
                Instance?.AddTimer(1.0f, () => { if (prop != null && prop.IsValid) prop.AcceptInput("Kill"); });
            }
            catch
            {
                try { if (prop != null && prop.IsValid) prop.AcceptInput("Kill"); } catch { }
            }
        }

        private static string GetEnemyModel(CCSPlayerController player)
        {
            CCSPlayerController[] models = [.. Utilities.GetPlayers().FindAll(p => p.IsValid && p.PawnIsAlive && p.Team != player.Team)];
            if (models != null && models.Length > 0)
            {
                var index = Instance?.Random.Next(models.Length) ?? 0;
                var model = models[index];
                if (model != null && model.IsValid && model.PlayerPawn.Value != null && model.PlayerPawn.Value.IsValid && model.PlayerPawn.Value.CBodyComponent != null && model.PlayerPawn.Value.CBodyComponent.SceneNode != null)
                {
                    string modelName = model.PlayerPawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;
                    if (!string.IsNullOrEmpty(modelName)) return modelName;
                }
            }
            return player.Team == CsTeam.CounterTerrorist ? defaultTModel : defaultCTModel;
        }

        private static void SetPlayerModel(CCSPlayerController player, string model)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            Server.NextFrame(() =>
            {
                pawn.SetModel(model);

                var originalRender = pawn.Render;
                pawn.Render = Color.FromArgb(255, originalRender.R, originalRender.G, originalRender.B);
            });
        }
    }
}