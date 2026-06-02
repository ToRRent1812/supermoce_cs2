using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Collections.Concurrent;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ReZombie : ISkill
    {
        private const Skills skillName = Skills.ReZombie;
        private static readonly ConcurrentDictionary<CCSPlayerController, byte> zombies = [];
        private static readonly object setLock = new();

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, 
            "Zombie", 
            "Po śmierci odradzasz się jako zombie z nożem i dużą ilością zdrowia", 
            "#ff5C0A");
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            zombies.TryRemove(player, out _);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            zombies.TryRemove(player, out _);
            SetPlayerColor(player.PlayerPawn.Value, true);
            ResetHealth(player);
            player.PlayerPawn.Value.VelocityModifier = 1f;
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = @event.Userid;
            var weapon = @event.Item;
            if(player == null || weapon == null) return;
            if (Instance?.IsPlayerValid(player) == false) return;
            if (!zombies.ContainsKey(player) || weapon == "c4" || weapon.Contains("knife") || weapon.Contains("bayonet")) return;
            player.ExecuteClientCommand("slot3");
        }

        public static void NewRound()
        {
            foreach (var player in zombies.Keys)
                DisableSkill(player);
            zombies.Clear();
        }

        public static void PlayerHurt(EventPlayerHurt @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || zombies.ContainsKey(player)) return;

            var playerInfo = SkillUtils.GetPlayerInfo(player);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn.Health > 0) return;

            lock (setLock)
            {
                bool isBlock = player.TeamChanged;
                zombies.TryAdd(player, 0);
                if(isBlock) return;
                SetPlayerColor(pawn, false);
                player.ExecuteClientCommand("slot3");
                SkillUtils.AddHealth(pawn, 750, 750);
                SkillUtils.PrintToChat(player, $"Jesteś zombie! Możesz używać tylko noża");
            }
        }

        public static void ResetHealth(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn?.Value;
            if (pawn == null) return;

            pawn.MaxHealth = 100;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            pawn.Health = Math.Min(pawn.Health, 100);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        private static void SetPlayerColor(CCSPlayerPawn pawn, bool normal)
        {
            var color = normal ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 255, 0, 0);
            pawn.Render = color;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }
}
