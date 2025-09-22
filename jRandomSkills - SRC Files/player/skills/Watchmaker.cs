using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using jRandomSkills.src.utils;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class Watchmaker : ISkill
    {
        private const Skills skillName = Skills.Watchmaker;
        private static readonly int roundTime = Config.GetValue<int>(skillName, "changeRoundTime");
        private static bool bombPlanted = false;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void NewRound()
        {
            bombPlanted = false;
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            bombPlanted = true;
        }

        public static void OnEntitySpawned(CEntityInstance entity)
        {
            if (bombPlanted) return;
            var name = entity.DesignerName;
            if (!name.EndsWith("_projectile"))
                return;

            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid) return;

            var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
            if (pawn == null || !pawn.IsValid || pawn.Controller == null || !pawn.Controller.IsValid || pawn.Controller.Value == null || !pawn.Controller.Value.IsValid) return;
            var player = pawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName || Instance.GameRules == null) return;

            Instance.GameRules.RoundTime += player.Team == CsTeam.Terrorist ? roundTime : -roundTime;
            /*if (player.Team == CsTeam.Terrorist)
                Server.PrintToChatAll($" {ChatColors.Orange}{Localization.GetTranslation("watchmaker_tt", roundTime)}");
            else
                Server.PrintToChatAll($" {ChatColors.LightBlue}{Localization.GetTranslation("watchmaker_ct", roundTime)}");*/
        }

        public static void OnTick()
        {
            if (bombPlanted) return;
            foreach (var player in Utilities.GetPlayers())
            {
                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
                if (playerInfo?.Skill == skillName)
                    UpdateHUD(player);
            }
        }

        private static void UpdateHUD(CCSPlayerController player)
        {
            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null || Instance?.GameRules == null || Instance?.GameRules?.RoundTime == null || Instance.GameRules?.RoundStartTime == null) return;

            int seconds = 1 + (int)(Instance.GameRules.RoundTime - (Server.CurrentTime - Instance.GameRules.RoundStartTime));

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name} ({SkillUtils.SecondsToTimer(seconds)})</font> <br>";
            string remainingLine = $"<font class='fontSize-s' class='fontWeight-Bold' color='#ffffff'>{skillData.Description}</font> ";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ff462e", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int changeRoundTime = 15) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int ChangeRoundTime { get; set; } = changeRoundTime;
        }
    }
}