using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class InfiniteAmmo : ISkill
    {
        private const Skills skillName = Skills.InfiniteAmmo;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, Config.GetValue<string>(skillName, "color"));

            Instance.RegisterEventHandler<EventWeaponFire>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName)
                    ApplyInfiniteAmmo(player!);

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventGrenadeThrown>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName)
                    player!.GiveNamedItem($"weapon_{@event.Weapon}");

                return HookResult.Continue;
            });

            Instance.RegisterEventHandler<EventWeaponReload>((@event, info) =>
            {
                var player = @event.Userid;

                if (player == null || !Instance.IsPlayerValid(player)) return HookResult.Continue;

                var playerInfo = Instance.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID);

                if (playerInfo?.Skill == skillName)
                    ApplyInfiniteAmmo(player!);
                
                return HookResult.Continue;
            });
        }

        private static void ApplyInfiniteAmmo(CCSPlayerController player)
        {
            var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
            if (activeWeaponHandle?.Value != null)
            {
                activeWeaponHandle.Value.Clip1 = 100;
            }
        }

        public class SkillConfig(Skills skill = skillName, bool active = true, string color = "#0000FF", CsTeam onlyTeam = CsTeam.None, bool needsTeammates = false) : Config.DefaultSkillInfo(skill, active, color, onlyTeam, needsTeammates)
        {
        }
    }
}