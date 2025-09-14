using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Impostor : ISkill
    {
        private const Skills skillName = Skills.Impostor;
        private static readonly string defaultCTModel = "characters/models/ctm_sas/ctm_sas.vmdl";
        private static readonly string defaultTModel = "characters/models/tm_phoenix_heavy/tm_phoenix_heavy.vmdl";

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            string model = GetEnemyModel(player);
            SetPlayerModel(player, model);
            Server.NextFrame(() => { player.Respawn(); });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            SetPlayerModel(player, player.Team == CsTeam.Terrorist ? defaultTModel : defaultCTModel);
        }

        private static string GetEnemyModel(CCSPlayerController player)
        {
            CCSPlayerController[] models = [.. Utilities.GetPlayers().FindAll(p => p.IsValid && p.PawnIsAlive && p.Team != player.Team)];
            if (models != null && models.Length > 0)
            {
                var model = models[Instance.Random.Next(models.Length)];
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

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#99140B", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}