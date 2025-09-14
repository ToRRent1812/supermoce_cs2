using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class PsychicDefusing : ISkill
    {
        private const Skills skillName = Skills.PsychicDefusing;
        private static readonly Dictionary<CCSPlayerPawn, PlayerSkillInfo> SkillPlayerInfo = [];
        private static Vector? bombLocation = null;
        private static readonly float maxDefusingRange = Config.GetValue<float>(skillName, "maxDefusingRange");
        private static readonly float defusingTime = Config.GetValue<float>(skillName, "defusingTime");
        private static readonly float tickRate = 64f;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            SkillPlayerInfo.Clear();
            bombLocation = null;
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
                SkillPlayerInfo.Remove(pawn);
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            if (plantedBomb != null)
                bombLocation = plantedBomb.AbsOrigin;
        }

        public static void OnTick()
        {
            if (bombLocation == null) return;
            foreach (var skillInfo in SkillPlayerInfo)
            {
                var player = skillInfo.Key;
                var info = skillInfo.Value;

                if (player.AbsOrigin == null || SkillUtils.GetDistance(player.AbsOrigin, bombLocation) > maxDefusingRange)
                {
                    info.Defusing = false;
                    info.DefusingTime = defusingTime;
                    continue;
                }

                if (!info.Defusing)
                    player.EmitSound("c4.disarmstart");
                info.Defusing = true;
                info.DefusingTime -= (1f / tickRate);

                if (info.DefusingTime <= 0)
                {
                    var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                    if (plantedBomb != null)
                    {
                        plantedBomb.AcceptInput("Kill");
                        SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
                    }

                    SkillPlayerInfo.Clear();
                }

                var playerController = player.Controller.Value;
                if (playerController == null || !player.Controller.IsValid) return;
                UpdateHUD(playerController.As<CCSPlayerController>(), info);
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillPlayerInfo[pawn] = new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                Defusing = false,
                DefusingTime = defusingTime,
            };
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillPlayerInfo.Remove(pawn);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            if (!skillInfo.Defusing) return;
            int cooldown = (int)skillInfo.DefusingTime + 1;

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font> <br>";
            string remainingLine = cooldown != 0 ? $"<font class='fontSize-m' color='#FFFFFF'>{Localization.GetTranslation("psychicdefusing_hud_info", $"<font color='#00d5ff'>{cooldown}</font>")}</font>" : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }
        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool Defusing { get; set; }
            public float DefusingTime { get; set; }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#507529", CsTeam onlyTeam = CsTeam.CounterTerrorist, bool needsTeammates = false, float maxDefusingRange = 200f, float defusingTime = 10f) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public float MaxDefusingRange { get; set; } = maxDefusingRange;
            public float DefusingTime { get; set; } = defusingTime;
        }
    }
}