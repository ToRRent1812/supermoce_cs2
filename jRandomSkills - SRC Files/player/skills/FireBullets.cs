using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class FireBullets : ISkill, IPassiveSkill
    {
        private const Skills skillName = Skills.FireBullets;
        private static readonly ConcurrentDictionary<CCSPlayerPawn, int> InfectedPlayers = [];
        private static readonly object setLock = new();
        private static readonly Color infectedColor = Color.FromArgb(100, 0, 200, 0);
        private static readonly Color normalColor = Color.FromArgb(255, 255, 255, 255);

        public static void LoadSkill()
        {
            SkillUtils.RegisterPassiveSkill(
                skillName,
                "Trujące Ammo",
                "Twoje pociski trują przeciwników",
                "#23771c",
                minValue: 2,
                maxValue: 7,
                step: 1,
                customValueFormatter: value => $"{value} sek.");
        }

        public static void NewRound()
        {
            PassiveSkillFramework.OnNewRound();

            lock (setLock)
            {
                foreach (var infectedPawn in InfectedPlayers.Keys)
                    ResetPlayerRenderColor(infectedPawn);

                InfectedPlayers.Clear();
            }
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            PassiveSkillFramework.OnSkillEnabled(skillName, player, config);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;
            PassiveSkillFramework.OnSkillDisabled(skillName, player);
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill == skillName)
            {
                if (InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _))
                    ResetPlayerRenderColor(player.PlayerPawn.Value);
            }
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            if (victim == null || attacker == null || victim.PlayerPawn.Value == null || victim.IsBot || !victim.IsValid || !victim.PawnIsAlive || Instance?.IsPlayerValid(attacker) == false || Instance?.IsPlayerValid(victim) == false)
                return;
            if (attacker == victim || attacker.TeamNum == victim.TeamNum)
                return;

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo?.Skill != skillName) return;

            var config = SkillUtils.GetPassiveSkillConfig(skillName);
            if (config == null) return;

            int durationSeconds = PassiveSkillFramework.GetRandomRoll(skillName, attacker, config);
            if (durationSeconds <= 0) return;

            InfectedPlayers.AddOrUpdate(victim.PlayerPawn.Value, Server.TickCount + durationSeconds * 64, (k, v) => Server.TickCount + durationSeconds * 64);
            MarkPlayerInfected(victim.PlayerPawn.Value);
        }

        public static void OnTick()
        {
            if (Server.TickCount % 64 != 0) return;
            foreach (var player in Utilities.GetPlayers().FindAll(p => p != null && p.PawnIsAlive))
            {
                if (player == null || player.PlayerPawn.Value == null) continue;
                if (InfectedPlayers.TryGetValue(player.PlayerPawn.Value, out int expiryTick))
                {
                    if (!player.IsValid || !player.PawnIsAlive)
                    {
                        if (InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _))
                            ResetPlayerRenderColor(player.PlayerPawn.Value);
                        continue;
                    }

                    if (Server.TickCount >= expiryTick)
                    {
                        if (InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _))
                            ResetPlayerRenderColor(player.PlayerPawn.Value);
                        continue;
                    }
                    else
                    {
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn == null || !playerPawn.IsValid) continue;
                        MarkPlayerInfected(playerPawn);
                        SkillUtils.TakeHealth(playerPawn, Instance?.Random.Next(2, 6) ?? 2);
                        playerPawn.EmitSound("Player.DamageBody.Onlooker", volume: 0.1f);
                        if (playerPawn.Health <= 0)
                        {
                            if (InfectedPlayers.TryRemove(player.PlayerPawn.Value, out _))
                                ResetPlayerRenderColor(player.PlayerPawn.Value);
                            continue;
                        }
                    }
                }
            }
        }

        private static void MarkPlayerInfected(CCSPlayerPawn pawn)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.Render = infectedColor;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        private static void ResetPlayerRenderColor(CCSPlayerPawn pawn)
        {
            if (pawn == null || !pawn.IsValid) return;
            pawn.Render = normalColor;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }
}
