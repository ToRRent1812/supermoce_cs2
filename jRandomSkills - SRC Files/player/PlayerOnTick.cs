using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static CounterStrikeSharp.API.Core.Listeners;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public static class PlayerOnTick
    {
        public static void Load()
        {
            Instance?.RegisterListener<OnTick>(() =>
            {
                UpdateGameRules();
                CacheValidPlayers();
                foreach (var validPlayer in Instance?.CachedValidPlayers ?? [])
                    UpdatePlayerHud(validPlayer);
            });

            Instance?.RegisterListener<OnMapStart>(OnMapStart);
        }

        private static void OnMapStart(string mapName)
        {
            if (Instance != null)
                Instance.GameRules = null;
        }

        private static void InitializeGameRules()
        {
            if (Instance?.GameRules != null) return;
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (Instance != null && gameRulesProxy != null && gameRulesProxy.GameRules != null)
                Instance.GameRules = gameRulesProxy.GameRules;
        }

        private static void UpdateGameRules()
        {
            if (Instance?.GameRules == null || Instance.GameRules.Handle == IntPtr.Zero)
                InitializeGameRules();
        }

        private static void CacheValidPlayers()
        {
            if (Instance != null)
                Instance.CachedValidPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV).ToArray();
        }

        private static void UpdatePlayerHud(CCSPlayerController player)
        {
            if (player == null) return;
            if (Instance?.SkillPlayerDict == null || !Instance.SkillPlayerDict.TryGetValue(player.SteamID, out var skillPlayer)) return;
            if (skillPlayer == null) return;

            string infoLine = "";
            string skillLine = "";

            if (SkillData.Skills.Count == 0 || SkillUtils.IsWarmup())
            {
                skillPlayer.IsDrawing = false;
                skillPlayer.Skill = Skills.None;
            }
            else if (skillPlayer.IsDrawing && !SkillUtils.IsWarmup())
            {
                var randomSkill = SkillData.Skills.ToArray()[Instance?.Random.Next(SkillData.Skills.Count) ?? 0];
                infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>Losowanie supermocy...</font> <br>";
                skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{randomSkill.Color}'>{randomSkill.Name}</font>";
            }
            else if (!skillPlayer.IsDrawing && !SkillUtils.IsWarmup())
            {
                if (player.PawnIsAlive)
                {
                    var skillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == skillPlayer.Skill);
                    if (skillInfo != null)
                    {
                        infoLine = !string.IsNullOrEmpty(skillPlayer.RandomPercentage)
                            ? $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillInfo.Color}'>{skillInfo.Name} ({skillPlayer.RandomPercentage})</font> <br>"
                            : $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillInfo.Color}'>{skillInfo.Name}</font> <br>";
                        skillLine = $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillInfo.Description}</font>";
                    }
                }
                else
                {
                    var observerHud = GetObserverHud(player);
                    if (observerHud == null) return;
                    infoLine = observerHud.Value.infoLine;
                    skillLine = observerHud.Value.skillLine;
                }
            }

            if (string.IsNullOrEmpty(infoLine) && string.IsNullOrEmpty(skillLine)) return;
            player.PrintToCenterHtml(infoLine + skillLine);
        }

        private static (string infoLine, string skillLine)? GetObserverHud(CCSPlayerController player)
        {
            var pawn = player.Pawn?.Value;
            if (pawn == null) return null;

            var observedPlayer = Instance?.CachedValidPlayers?.FirstOrDefault(p => p?.Pawn?.Value?.Handle == pawn.ObserverServices?.ObserverTarget?.Value?.Handle);
            if (observedPlayer == null) return null;

            if (Instance?.SkillPlayerDict == null || !Instance.SkillPlayerDict.TryGetValue(observedPlayer.SteamID, out var observeredPlayerSkill)) return null;
            if (observeredPlayerSkill == null) return null;

            var skillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == observeredPlayerSkill.Skill);
            if (skillInfo == null) return null;

            var specialSkillInfo = SkillData.Skills.FirstOrDefault(s => s.Skill == observeredPlayerSkill.SpecialSkill);
            if (specialSkillInfo == null) return null;

            string pName = observeredPlayerSkill.PlayerName;
            if (pName.Length > 15)
                pName = $"{pName[..14]}...";

            var infoLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='#FFFFFF'>{pName} posiada</font> <br>";
            var skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillInfo.Color}'>{(observeredPlayerSkill.SpecialSkill == Skills.None ? skillInfo.Name : $"{specialSkillInfo.Name} -> {skillInfo.Name}")}</font> <br/> <font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillInfo.Description}</font>";
            return (infoLine, skillLine);
        }
    }
}