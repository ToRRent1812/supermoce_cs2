using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class PsychicDefusing : ISkill
    {
        private const Skills skillName = Skills.PsychicDefusing;
        private static readonly ConcurrentDictionary<CCSPlayerPawn, PlayerSkillInfo> SkillPlayerInfo = [];
        private static Vector? bombLocation = null;
        private static readonly float tickRate = 64f;
        private static nint bombDistance = 0;
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Medium", "Rozbrajasz bombę zdalnie w obszarze 30m", "#507529", 2, 1);
        }

        public static void NewRound()
        {
            lock (setLock)
            {
                SkillPlayerInfo.Clear();
                bombLocation = null;
            }
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill == skillName)
                SkillPlayerInfo.TryRemove(pawn, out _);
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
                var pawn = skillInfo.Key;
                var info = skillInfo.Value;

                var playerController = pawn.Controller.Value;
                if (playerController == null || !pawn.Controller.IsValid) return;

                var player = playerController.As<CCSPlayerController>();
                if (player == null || !player.IsValid) return;

                if(pawn.AbsOrigin != null) bombDistance = (nint)SkillUtils.GetDistance(pawn.AbsOrigin, bombLocation);

                if (pawn.AbsOrigin == null || bombDistance > 1200)
                {
                    info.Defusing = false;
                    info.DefusingTime = 13f;
                    continue;
                }

                if (!info.Defusing)
                    pawn.EmitSound("c4.disarmstart");
                info.Defusing = true;
                info.DefusingTime -= 1f / tickRate;

                if (info.DefusingTime <= 0)
                {
                    var plantedBomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
                    if (plantedBomb != null)
                    {
                        plantedBomb.AcceptInput("Kill");
                        SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
                        SkillUtils.PrintToChatAll($" {ChatColors.LightBlue}{player.PlayerName} rozbroił zdalnie bombę.", false);
                    }
                    SkillPlayerInfo.Clear();
                }

                UpdateHUD(player, info);
            }
        }

         public static void EnableSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillPlayerInfo.TryAdd(pawn, new PlayerSkillInfo
            {
                SteamID = player.SteamID,
                Defusing = false,
                DefusingTime = 13f,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillPlayerInfo.TryRemove(pawn, out _);
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            if (!skillInfo.Defusing) return;
            float DefusingPercent = Math.Clamp((1f - (skillInfo.DefusingTime / 13f)) * 100f, 0f, 100f);
            float DistPercent = Math.Clamp((1f - (bombDistance / 1200f)) * 100f, 0f, 100f);

            var skillData = SkillData.Skills.FirstOrDefault(s => s.Skill == skillName);
            if (skillData == null) return;

            string skillLine = $"<font class='fontSize-m' class='fontWeight-Bold' color='{skillData.Color}'>{skillData.Name}</font><br>";
            string remainingLine = DefusingPercent < 100f
                ? $"<font class='fontSize-m' color='#b5ffee'>Postęp: <font color='#00ff00'>{DefusingPercent:0}%  |  Zasięg: {DistPercent:0}%</font></font><br>"
                : $"<font class='fontSize-s' class='fontWeight-Bold' color='#FFFFFF'>{skillData.Description}</font>";

            var hudContent = skillLine + remainingLine;
            player.PrintToCenterHtml(hudContent);
        }
        public class PlayerSkillInfo
        {
            public ulong SteamID { get; set; }
            public bool Defusing { get; set; }
            public float DefusingTime { get; set; }
        }
    }
}