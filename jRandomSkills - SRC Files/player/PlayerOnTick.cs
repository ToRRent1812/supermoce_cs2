using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
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
            //Instance.GameRules = null;
            Event.staticSkills.Clear();
        }

        /*private static void InitializeGameRules()
        {
            if (Instance.GameRules != null) return;
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy != null)
                Instance.GameRules = gameRulesProxy?.GameRules;
        }

        private static void UpdateGameRules()
        {
            if (Instance?.GameRules == null || Instance?.GameRules?.Handle == IntPtr.Zero)
                InitializeGameRules();
            else if (Instance != null)
                Instance.GameRules.GameRestart = Instance.GameRules?.RestartRoundTime < Server.CurrentTime;
        }*/

        private static void UpdatePlayerHud(CCSPlayerController player)
        {
            if (player == null) return;
            var skillPlayer = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (skillPlayer == null) return;

            string infoLine = "";
            string skillLine = "";

            if (SkillData.Skills.Count == 0)
            {
                infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("your_skill")}:</font> <br>";
                skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("none")}</font>";
            }
            else if (skillPlayer.IsDrawing)
            {
                var randomSkill = SkillData.Skills[Instance.Random.Next(SkillData.Skills.Count)];
                infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("drawing_skill")}</font> <br>";
                skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{randomSkill.Color}'>{randomSkill.Name}</font>";
            }
            else if (!skillPlayer.IsDrawing)
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

                    string pName = observeredPlayerSkill.PlayerName;
                    if (pName.Length > 18)
                        pName = $"{pName[..17]}...";
                    infoLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='#FFFFFF'>{Localization.GetTranslation("observer_skill")} {pName}:</font> <br>";
                    skillLine = $"<font class='fontSize-l' class='fontWeight-Bold' color='{observeredPlayerSkillInfo.Color}'>{(observeredPlayerSkill.SpecialSkill == Skills.None ? observeredPlayerSkillInfo.Name : $"{observeredPlayerSpecialSkillInfo.Name}({observeredPlayerSkillInfo.Name})")}</font> <br>";
                }
            }

            if (string.IsNullOrEmpty(infoLine) && string.IsNullOrEmpty(skillLine)) return;
            var hudContent = infoLine + skillLine;
            if (player == null || !player.IsValid) return;
            player.PrintToCenterHtml(hudContent);
        }
    }
}