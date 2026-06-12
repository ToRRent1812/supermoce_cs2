using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Supermoce.src.player;
using System.Collections.Concurrent;
using static Supermoce.Supermoce;

namespace Supermoce
{
    public class TeleDecoy : ISkill
    {
        private const Skills skillName = Skills.TeleDecoy;

        private static readonly ConcurrentDictionary<CCSPlayerController, bool> decoyActive = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(
                skillName,
                "TeleWabik",
                "Przenosisz się na miejsce, gdzie upadnie twój wabik",
                "#5e3bfa");
        }

        public static void NewRound()
        {
            decoyActive.Clear();
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            if (Instance?.IsPlayerValid(player) == false) return;

            decoyActive[player] = false;
            SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null) return;

            decoyActive.TryRemove(player, out _);
        }

        public static void DecoyStarted(EventDecoyStarted @event)
        {
            var player = @event.Userid;
            if (player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            if (decoyActive.TryGetValue(player, out var active) && active) return;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) return;

            decoyActive[player] = true;

            Vector pos = new(@event.X, @event.Y, @event.Z);

            pawn.Teleport(
                pos,
                pawn.AbsRotation,
                new Vector(0, 0, 0));
        }

        public static void DecoyDetonate(EventDecoyDetonate @event)
        {
            var player = @event.Userid;
            if(player == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            decoyActive[player] = false;

            Instance?.AddTimer(5.0f, () =>
            {
                if (player != null && player.IsValid && player.PawnIsAlive)
                {
                    SkillUtils.TryGiveWeapon(player, CsItem.DecoyGrenade);
                }
            });
        }
    }
}