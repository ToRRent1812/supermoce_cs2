using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using jRandomSkills.src.player;
using static jRandomSkills.jRandomSkills;

namespace jRandomSkills
{
    public class HolyHandGrenade : ISkill
    {
        private const Skills skillName = Skills.HolyHandGrenade;

        public static void LoadSkill()
        {
            SkillUtils.RegisterSkill(skillName, "Święty Granat Ręczny", "Potrójny zasięg i obrażenia z twoich granatów wybuchowych", "#ffdd00");
        }

        public static void OnEntitySpawned(CEntityInstance @event)
        {
            var name = @event.DesignerName;
            if (!name.EndsWith("hegrenade_projectile"))
                return;

            Server.NextFrame(() =>
            {
                var hegrenade = @event.As<CHEGrenadeProjectile>();
                if (hegrenade == null || !hegrenade.IsValid) return;

                var playerPawn = hegrenade.Thrower.Value;
                if (playerPawn == null || !playerPawn.IsValid) return;

                var player = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerPawn.Index == playerPawn.Index);
                var playerInfo = Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player?.SteamID);
                if (playerInfo?.Skill != skillName) return;

                hegrenade.Damage *= 3f;
                hegrenade.DmgRadius *= 3f;
            });
        }

        public static void EnableSkill(CCSPlayerController player)
        {
            SkillUtils.TryGiveWeapon(player, CsItem.HEGrenade);
        }
    }
}