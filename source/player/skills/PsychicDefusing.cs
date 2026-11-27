using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class PsychicDefusing : ISkill
    {
        private const Skills skillName = Skills.PsychicDefusing;
        private static readonly ConcurrentDictionary<CCSPlayerPawn, PlayerSkillInfo> SkillPlayerInfo = [];
        private static Vector? bombLocation = null;
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(skillName, 
            "Medium", 
            "Rozbrajasz bombę zdalnie", 
            "#507529", 
            teamnum:2,
            objective:1,
            minValue:15,
            maxValue:40,
            step:1,
            customValueFormatter: (value) => $"{value}m");
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
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
                SkillPlayerInfo.TryRemove(pawn, out _);
        }

        public static void BombPlanted(EventBombPlanted _)
        {
            bombLocation = Bomb.Planted?.AbsOrigin;
        }

        public static void OnTick()
        {
            if (bombLocation == null)
            {
                bombLocation = Bomb.Planted?.AbsOrigin;
                if (bombLocation == null) return;
            }
            foreach (var skillInfo in SkillPlayerInfo)
            {
                var pawn = skillInfo.Key;
                var info = skillInfo.Value;

                var playerController = pawn.Controller.Value;
                if (playerController == null || !pawn.Controller.IsValid) return;

                var player = playerController.As<CCSPlayerController>();
                if (Instance?.IsPlayerValid(player) == false) return;

                float maxDefuseTime = GetDefuseTime(player);

                double currentDistance = 0;
                if (pawn.AbsOrigin != null)
                    currentDistance = SkillUtils.GetDistance(pawn.AbsOrigin, bombLocation);
                info.CurrentDistance = currentDistance;

                if (pawn.AbsOrigin == null || currentDistance > info.MaxDistanceHammer)
                {
                    info.Defusing = false;
                    info.DefusingTime = maxDefuseTime;
                    info.MaxDefusingTime = maxDefuseTime;
                    continue;
                }

                if (!info.Defusing)
                {
                    pawn.EmitSound("c4.disarmstart");
                    info.DefusingTime = maxDefuseTime;
                    info.MaxDefusingTime = maxDefuseTime;
                }

                info.Defusing = true;
                info.DefusingTime -= 1f / 64f;

                if (info.DefusingTime <= 0)
                {
                    if (Bomb.Planted != null)
                    {
                        Bomb.RemovePlanted();
                        SkillUtils.TerminateRound(CsTeam.CounterTerrorist);
                        SkillUtils.AwardRoundEndMoney(CsTeam.CounterTerrorist, player, RoundEndReason.BombDefused);
                        SkillUtils.PrintToChatAll($" {ChatColors.LightBlue}{player.PlayerName} rozbroił zdalnie bombę.", false);
                    }
                    SkillPlayerInfo.Clear();
                }

                UpdateHUD(player, info);
            }
        }

         public static void EnableSkill(CCSPlayerController player)
        {
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            float maxDistanceMeters = 30f;
            if (config != null)
            {
                PassiveSkillFramework.OnSkillEnabled(skillName, player, config);

                int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
                maxDistanceMeters = (float)randomValue;
            }
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || player.IsValid == false) return;
            float defuseTime = GetDefuseTime(player);
            SkillPlayerInfo.TryAdd(pawn, new PlayerSkillInfo
            {
                Slot = player.Slot,
                Defusing = false,
                DefusingTime = defuseTime,
                MaxDefusingTime = defuseTime,
                MaxDistance = maxDistanceMeters,
                MaxDistanceHammer = maxDistanceMeters / 0.025f,
            });
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            SkillPlayerInfo.TryRemove(pawn, out _);
            PassiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        private static float GetDefuseTime(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return 14f;
            return player.PawnHasDefuser ? 7f : 14f;
        }

        private static void UpdateHUD(CCSPlayerController player, PlayerSkillInfo skillInfo)
        {
            if (!skillInfo.Defusing) return;
            float defuseDuration = skillInfo.MaxDefusingTime > 0 ? skillInfo.MaxDefusingTime : 14f;
            float DefusingPercent = Math.Clamp((1f - (skillInfo.DefusingTime / defuseDuration)) * 100f, 0f, 100f);
            float Distance = (float)(skillInfo.CurrentDistance * 0.025f);

            string status = DefusingPercent < 100f
                ? $"Postęp: {DefusingPercent:0}%  |  Odległość: {Distance:0.0}m"
                : $"Zasięg: {skillInfo.MaxDistance:0}m";

            SkillHudPanel.ShowStatus(player, skillName, status);
        }
        public class PlayerSkillInfo
        {
            public int Slot { get; set; }
            public bool Defusing { get; set; }
            public float DefusingTime { get; set; }
            public float MaxDefusingTime { get; set; }
            public float MaxDistance { get; set; }
            public float MaxDistanceHammer { get; set; }
            public double CurrentDistance { get; set; }
        }
    }
}