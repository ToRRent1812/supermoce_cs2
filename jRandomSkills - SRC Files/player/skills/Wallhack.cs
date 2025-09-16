using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Wallhack : ISkill
    {
        private const Skills skillName = Skills.Wallhack;
        private static readonly List<(CDynamicProp, CDynamicProp)> glows = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == player?.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value?.Handle);
                var observerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer?.SteamID);

                if (playerInfo?.Skill == skillName || (observerInfo != null && observerInfo?.Skill == skillName))
                    continue;

                foreach (var glow in glows)
                {
                    info.TransmitEntities.Remove(glow.Item1);
                    info.TransmitEntities.Remove(glow.Item2);
                }
            }
        }

        public static void NewRound()
        {
            foreach (var glow in glows)
            {
                if (glow.Item1 != null && glow.Item1.IsValid)
                    glow.Item1.AcceptInput("Kill");
                if (glow.Item2 != null && glow.Item2.IsValid)
                    glow.Item2.AcceptInput("Kill");
            }
            glows.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.EnableTransmit();
            SetGlowEffectForEnemies(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo == null) return;

            playerInfo.Skill = Skills.None;
            if (!Instance.SkillPlayer.Any(s => s.Skill == skillName))   
                NewRound();
        }

        private static void SetGlowEffectForEnemies(CCSPlayerController player)
        {
            CsTeam originalTeam = player.Team;
            foreach (var enemy in Utilities.GetPlayers().FindAll(p => p.Team != originalTeam && p.PawnIsAlive))
            {
                var enemyInfo = Instance.SkillPlayer.FirstOrDefault(e => e.SteamID == enemy.SteamID);
                if (enemyInfo?.Skill == Skills.Ghost)
                    return;

                var enemyPawn = enemy.PlayerPawn.Value;
                var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

                if (modelGlow == null || modelRelay == null)
                    return;

                modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
                modelRelay.SetModel(enemyPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                modelRelay.Spawnflags = 256u;
                modelRelay.RenderMode = RenderMode_t.kRenderNone;
                modelRelay.DispatchSpawn();

                modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
                modelGlow.SetModel(enemyPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                modelGlow.Spawnflags = 256u;
                modelGlow.Render = Color.FromArgb(1, 255, 255, 255);
                modelGlow.DispatchSpawn();

                modelGlow.Glow.GlowColorOverride = enemy.Team == CsTeam.Terrorist ? Color.FromArgb(255, 255, 165, 0) : Color.FromArgb(255, 173, 216, 230);
                modelGlow.Glow.GlowRange = 5000;
                modelGlow.Glow.GlowTeam = -1;
                modelGlow.Glow.GlowType = 3;
                modelGlow.Glow.GlowRangeMin = 100;

                modelRelay.AcceptInput("FollowEntity", enemyPawn, modelRelay, "!activator");
                modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");
                glows.Add((modelRelay, modelGlow));
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#5d00ff", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}