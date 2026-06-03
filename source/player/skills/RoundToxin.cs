using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class RoundToxin : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.RoundToxin;

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Bąk morderca",
                "Wrogowie w twoim pobliżu otrzymują obrażenia",
                "#8b4e4e",
                minValue: 700,
                maxValue: 1300,
                step: 50,
                customValueFormatter: value => $"{value * 0.025:0.#}m");
        }

        public static void OnTick()
        {
            if (Server.TickCount % 128 != 0) return;
            if (SkillUtils.IsFreezetime()) return;

            var players = SkillUtils.CachedPlayers
                .Where(p => p.PawnIsAlive && p.PlayerPawn.Value != null && p.PlayerPawn.Value.IsValid)
                .ToArray();

            foreach (var player in players)
            {
                var playerInfo = SkillUtils.GetPlayerInfo(player);
                if (playerInfo?.Skill != skillName) continue;
                if (playerInfo.SkillChance == null || playerInfo.SkillChance <= 0) continue;

                var ownerPawn = player.PlayerPawn.Value;
                if (ownerPawn == null || !ownerPawn.IsValid || ownerPawn.AbsOrigin == null) continue;

                float range = playerInfo.SkillChance ?? 0;
                foreach (var target in players)
                {
                    if (target.SteamID == player.SteamID) continue;
                    if (target.Team == player.Team) continue;

                    var targetPawn = target.PlayerPawn.Value;
                    if (targetPawn == null || !targetPawn.IsValid || targetPawn.AbsOrigin == null) continue;

                    if (SkillUtils.GetDistance(ownerPawn.AbsOrigin, targetPawn.AbsOrigin) <= range)
                    {
                        int rngDamage = Instance?.Random.Next(1, 4) ?? 1;
                        SkillUtils.TakeHealth(targetPawn, rngDamage);
                    }
                }
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
            int randomValue = PassiveSkillFramework.GetRandomRoll(skillName, player, config);
            playerInfo.SkillChance = randomValue;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            PassiveSkillFramework.OnSkillDisabled(skillName, player);

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo == null) return;

            playerInfo.SkillChance = 0;
        }
    }
}
