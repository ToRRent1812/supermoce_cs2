using CounterStrikeSharp.API.Core;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class InfiniteAmmo : ISkill
    {
        private const Skills skillName = Skills.InfiniteAmmo;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Nieskończone Ammo", "Nielimitowana amunicja i granaty", "#0000FF");
        }

        public static void WeaponFire(EventWeaponFire @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill == skillName)
                ApplyInfiniteAmmo(player!);

        }

        public static void GrenadeThrown(EventGrenadeThrown @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill == skillName)
                player!.GiveNamedItem($"weapon_{@event.Weapon}");

        }

        public static void WeaponReload(EventWeaponReload @event)
        {
            var player = @event.Userid;
            if (Instance?.IsPlayerValid(player) == false) return;

            var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
            if (playerInfo?.Skill == skillName)
                ApplyInfiniteAmmo(player!);
        }

        private static void ApplyInfiniteAmmo(CCSPlayerController player)
        {
            var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
            if (activeWeaponHandle?.Value != null)
                activeWeaponHandle.Value.Clip1 = 100;
        }
    }
}