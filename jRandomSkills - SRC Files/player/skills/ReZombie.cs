using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using System.Drawing;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class ReZombie : ISkill
    {
        private const Skills skillName = Skills.ReZombie;
        private static readonly int zombieHealth = Config.GetValue<int>(skillName, "zombieHealth");
        private static readonly HashSet<CCSPlayerController> zombies = [];

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            zombies.Remove(player);
        }

        public static void DisableSkill(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) return;
            zombies.Remove(player);
            SetPlayerColor(player.PlayerPawn.Value, true);
        }

        public static void WeaponEquip(EventItemEquip @event)
        {
            var player = @event.Userid;
            var weapon = @event.Item;
            if (player == null || !player.IsValid) return;
            if (!zombies.Contains(player) || weapon == "c4") return;
            player.ExecuteClientCommand("slot3");
        }

        public static void NewRound()
        {
            foreach (var player in zombies)
                DisableSkill(player);
            zombies.Clear();
        }

        public static void PlayerDeath(EventPlayerDeath @event)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid || zombies.Contains(player)) return;

            var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);
            if (playerInfo?.Skill != skillName) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn.AbsOrigin == null) return;
            Vector deadPosition = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
            QAngle deadRotation = new(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);

            player.Respawn();
            Instance.AddTimer(.2f, () => {
                player.Respawn();
                zombies.Add(player);
                SetPlayerColor(pawn, false);
                SkillUtils.AddHealth(pawn, zombieHealth - 100, zombieHealth);
                pawn.Teleport(deadPosition, deadRotation);
                player.ExecuteClientCommand("slot3");
                Instance.AddTimer(1, () => player.ExecuteClientCommand("slot3"));
            });
        }

        public static void OnTick()
        {
            if (Server.TickCount % 16 != 0) return;
            foreach (var player in Utilities.GetPlayers())
            {
                if(player == null || !player.IsValid || !player.PawnIsAlive || !zombies.Contains(player)) continue;
                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;
                SkillUtils.AddHealth(pawn, 1, zombieHealth);
            }
        }

        private static void SetPlayerColor(CCSPlayerPawn pawn, bool normal)
        {
            var color = normal ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 255, 0, 0);
            pawn.Render = color;
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#ff5C0A", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false, int zombieHealth = 500) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
            public int ZombieHealth { get; set; } = zombieHealth;
        }
    }
}