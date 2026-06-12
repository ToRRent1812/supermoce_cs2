using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class Juiced : ISkill
    {
        private const Skills skillName = Skills.Juiced;
        private static readonly ConcurrentDictionary<CCSPlayerController, float> _damageBonus = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName,
            "Medytacja",
            "Co sekundę zadajesz większe obrażenia. Zabójstwo resetuje premię.",
            "#35ff3f");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            _damageBonus[player] = 0f;
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            _damageBonus.TryRemove(player, out _);
            var playerinfo = SkillUtils.GetPlayerInfo(player);
            if (playerinfo != null)
                playerinfo.RandomPercentage = null;
        }

        public static void OnTick()
        {
            if(Server.TickCount % 64 != 0) return;
            foreach (var player in _damageBonus.Keys)
            {
                _damageBonus[player] += 0.01f;
                var playerinfo = SkillUtils.GetPlayerInfo(player);
                if (playerinfo != null && playerinfo.Skill == skillName)
                    playerinfo.RandomPercentage = (int)(_damageBonus[player] * 100)+"%";
            }
        }

        public static HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || entity.Entity == null || info == null || info.Attacker == null || info.Attacker.Value == null)
                return HookResult.Continue;

            CCSPlayerPawn attackerPawn = new(info.Attacker.Value.Handle);
            CCSPlayerPawn victimPawn = new(entity.Handle);

            if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
                return HookResult.Continue;

            if (attackerPawn.Controller?.Value == null)
                return HookResult.Continue;

            CCSPlayerController attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

            var playerInfo = SkillUtils.GetPlayerInfo(attacker);
            if (playerInfo == null || playerInfo.Skill != skillName) return HookResult.Continue;
            if (_damageBonus.TryGetValue(attacker, out float bonus))
                info.Damage *= 1.0f + bonus;

            return HookResult.Continue;
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var killer = @event.Attacker;
            var victim = @event.Userid;
            if (killer == null || killer == victim || Instance?.IsPlayerValid(killer) == false || !killer.PawnIsAlive)
                return;

            var playerInfo = SkillUtils.GetPlayerInfo(killer);
            if (playerInfo?.Skill == skillName)
                _damageBonus[killer] = 0f;
        }

        public static void NewRound()
        {
            _damageBonus.Clear();
        }
    }
}
