using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class PlayerOnTick
    {
        public static void Load()
        {
            Instance.RegisterListener<OnTick>(() =>
            {
                //UpdateGameRules();
                foreach (var player in Utilities.GetPlayers())
                    if (player != null && player.IsValid)
                        UpdatePlayerHud(player);
            });

            Instance.RegisterListener<OnMapStart>(OnMapStart);
        }

        private static void OnMapStart(string mapName)
        {
            Event.staticSkills.Clear();
            //Instance.GameRules = null;
        }
        /*private static void InitializeGameRules()
        {
            if (Instance.GameRules != null) return;
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy != null)
                Instance.GameRules = gameRulesProxy?.GameRules;
        }*/

        /*private static void UpdateGameRules()
        {
            if (Instance.GameRules == null)
                InitializeGameRules();
            else
                Instance.GameRules.GameRestart = Instance.GameRules.RestartRoundTime < Server.CurrentTime;
        }*/

        private static void UpdatePlayerHud(CCSPlayerController player)
        {
            if (player == null) return;

            var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (skillPlayer == null) return;

            string infoLine = "";
            string skillLine = "";

            if (SkillData.Skills.Count == 0 || SkillUtils.IsWarmup())
            {
                infoLine = "";
                skillLine = "";
                skillPlayer.IsDrawing = false;
                skillPlayer.Skill = src.player.Skills.None;
            }
            else if (skillPlayer.IsDrawing && !SkillUtils.IsWarmup())
            {
                var playerPawn = player.PlayerPawn?.Value;
                // Reset dwarf in freezetime
                if (playerPawn != null && playerPawn.CBodyComponent.SceneNode.Scale < 1f)
                {
                    playerPawn.CBodyComponent.SceneNode.Scale = 1f;
                    Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
                }
                // Reset ghost in freezetime
                if (playerPawn != null && playerPawn.Render != Color.FromArgb(255, 255, 255, 255))
                {
                    playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
                    playerPawn.ShadowStrength = 1.0f;
                    Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
                }
                // Reset Astronaut in freezetime
                if (playerPawn != null && playerPawn.ActualGravityScale != 1.0f)
                {
                    playerPawn.ActualGravityScale = 1.0f;
                    //Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flGravityScale");
                }
                // Reset speed in freezetime
                if (playerPawn != null && playerPawn.VelocityModifier != 1.0f)
                {
                    playerPawn.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }

                // Reset FOV in freezetime
                if (player != null && player.DesiredFOV != 90)
                {
                    player.DesiredFOV = 90;
                    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                }

                var randomSkill = SkillData.Skills[Instance.Random.Next(SkillData.Skills.Count)];
                infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("drawing_skill")}</font> <br>";
                skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{randomSkill.Color}'>{randomSkill.Name}</font>";
            }
            else if (!skillPlayer.IsDrawing && !SkillUtils.IsWarmup())
            {
                if (player?.IsValid == true && player?.PawnIsAlive == true)
                {
                    var skillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == skillPlayer.Skill);
                    if (skillInfo != null)
                    {
                        infoLine = !string.IsNullOrEmpty(skillPlayer.RandomPercentage) ? $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillInfo.Color}'>{skillInfo.Name} ({skillPlayer.RandomPercentage})</font> <br>" : $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillInfo.Color}'>{skillInfo.Name}</font> <br>";
                        skillLine = $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillInfo.Description}</font>";
                    }
                } else if (player?.IsValid == true)
                {
                    var pawn = player.Pawn.Value;
                    if (pawn == null) return;

                    var observedPlayer = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == pawn?.ObserverServices?.ObserverTarget?.Value?.Handle);
                    if (observedPlayer == null) return;

                    var observeredPlayerSkill = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == observedPlayer.SteamID);
                    if (observeredPlayerSkill == null) return;
                    
                    var observeredPlayerSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == observeredPlayerSkill.Skill);
                    if (observeredPlayerSkillInfo == null) return;

                    var observeredPlayerSpecialSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == observeredPlayerSkill.SpecialSkill);
                    if (observeredPlayerSpecialSkillInfo == null) return;

                    infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{observeredPlayerSkill.PlayerName} {Localization.GetTranslation("observer_skill")}</font> <br>";
                    skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{observeredPlayerSkillInfo.Color}'>{(observeredPlayerSkill.SpecialSkill == Skills.None ? observeredPlayerSkillInfo.Name : $"{observeredPlayerSpecialSkillInfo.Name}({observeredPlayerSkillInfo.Name})")}</font> <br> <font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{observeredPlayerSkillInfo.Description}</font>";
                }
            }

            if (string.IsNullOrEmpty(infoLine) && string.IsNullOrEmpty(skillLine)) return;

            var hudContent = infoLine + skillLine;

            if (player == null || !player.IsValid) return;
            player.PrintToCenterHtml(hudContent);
        }
    }
}
